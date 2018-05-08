En la segunda parte explicaremos como usar la captcha en la parte del cliente de forma segura para que el sistema no sea vulnerado.

Como ya hemos dicho md5 no es una opción válida, todo lo que hagamos tendrá que ser parte del servidor, y bien, tenemos una muy buena opción que son las sesiones de PHP.

En un principio, no sabía como integrarlo, pero ya he visto que PHP puede resumir una sesión si se le pasa como POST, GET o por cookie, como bien dice [la documentación](http://php.net/manual/es/function.session-start.php).

Pues bien, empecemos, lo primero que tenemos que hacer es crear el cliente, para ello creamos un nuevo proyecto en Visual Studio. Una vez ene ste punto, instalaremos unos paquetes en la consola de Nuget.

* https://www.nuget.org/packages/Flurl.Http
* https://www.nuget.org/packages/newtonsoft.json/

Con el primero lanzaremos las peticiones ([gracias a estas extensiones que yo he mismo he creado y he publicado en Gist Github](https://gist.github.com/z3nth10n/7a2574da2ce7f3afec4edfc1af1c5478)) HTTP hacia nuestra API.

Y con el segundo podremos leer las respuestas que la API nos envía.

Bueno, pues empezemos a fabicar.

Lo más curioso de todo esto es que yo empecé todo dentro de una consola, y claro, dicha consola la tengo para hacer pruebas de seguridad. Mientras razonaba me di cuenta de que me hacia falta un sistema de captcha, y como estaba dentro de una consola quise hacer la gracia de que la captcha estuviese dentro de la consola también.

Primero explicaremos como descargarnos nuestra imagen:

	private static Image DownloadReCaptcha(FlurlClient client, ref JObject challenge)
    {
        try
        {
            string res = client.HttpGet("http://localhost/z/api.php?action=captcha");

            challenge = JsonConvert.DeserializeObject<JObject>(res);

            byte[] byteArrayIn = Convert.FromBase64String(challenge["data"]["jpeg"].ToString());

            using (var ms = new MemoryStream(byteArrayIn))
                return Image.FromStream(ms);
        }
        catch (Exception)
        {
            challenge = null;
            return null;
        }
    }

Expliquemos parte por parte que hemos hecho.

Primero en los parametros, necesitaremos un cliente Flurl ya que luego compartiremos las cookies por el tema de las sesiones.

En el segundo parametro pasaremos por referencia hacia a fuera del método el objeto entero para luego ir comparando.

Con dicho cliente formaremos una petición GET a la página de la API.

Lo que nos devuelva lo convertiremos a un objeto JSON.

Y buscaremos dentro de este objecto el índice que contiene los datos del jpeg, que si bien recordamos estaban convertidos a base64, por lo que haremos el proceso inverso, y una vez con los bytes, solo tendremos que dumpearlos dentro de un Stream para que este solito lo vuelque en una imagen, sencillo ¿no?

Una vez con la imagen y la información de la página podemos hacer lo que realmente se nos antoje, volcarlo dentro de un PictureBox dentro de WinForms/WPF o más guay aún, volcarlo dentro de la consola. ¿Y como?

Estas 2 respuestas de Stackoverflow tienen la solución:

* https://stackoverflow.com/a/33715138/3286975 (esta utiliza chars para dibujar la imagen, pero por lo general no será humanamente leíble)
* https://stackoverflow.com/a/33652557/3286975 (esta utiliza la API de Windows para visualizar la imagen tal como se ve dentro de la consola)

A la segunda respuesta, hay que hacerle un par de arreglos, ya que si minizamos la aplicación o seleccionamos la imagen esta desaparece, porque necesita ser actualizada.

Pero lo más importante, no es solo dibujar la imagen, tendremos que responder y saber si lo que respondemos está bien, para ello viene la segunda parte del método.

	public static string SolveCaptcha()
    {
        using (FlurlClient client = new FlurlClient().EnableCookies())
        {
            JObject challenge = null;
            Image img = DownloadReCaptcha(client, ref challenge);

            // Aquí mostriamos la imagen en consola...

            Console.Write("What's in the captcha?: ");
            string r = Console.ReadLine();

            return client.HttpPost(ApiUrl, new { action = "resolve-captcha", input = r, PHPSESSID = client.Cookies.First().Value.Value }, false);
        }
    }

Vale, expliquemos todo lo que hemos hecho.

Primero declaramos el cliente Flurl, dentro de un using puesto que hereda a IDisposable.

Después declaramos el JObject el cual asignaremos por referencia.

Llamaremos al metodo que se descarga la imagen, y ya haremos lo que veamos, por ejemplo, yo hago lo siguiente:

	int height = 5;
    for (int i = 0; i < height; ++i)
        Console.WriteLine();


    //Este es el metodo de la segunda respuesta, la imagen que se ve tal cual...
    ConsoleDisplayImage(img, new Point(0, 0), new Size((int)(height * 4.5f), height));

	//Este es el metodo de la primera respuesta, la que utiliza caracteres para mostrar la imagen
    Console.WriteLine();
    ConsoleWriteImage((Bitmap)img);
    Console.WriteLine();

Y por último, siguiendo con la parte de arriba, solo tendremos que pedir al usuario que rellene lo que ve, y enviarlo por POST, que aquí viene lo peliagudo.

Para que la sesión PHP puede seguir existiendo dentro de PHP en la siguiente petición, tenemos que volver a enviarla tal cual, de ahí el motivo de la extencia de esta linea: `PHPSESSID = client.Cookies.First().Value.Value`

Obviamente, todo esto, ha supuesto un pequeño cambio en el PHP:

	$coreData["md5"] = md5($phrase);

Esta línea la hemnos dejado de enviar, primero porque no usamos msd5 y segundo porque no es logico que el cliente tenga cceso a la solución.

Ahora al principio tenemos la siguiente línea:

    if (session_status() == PHP_SESSION_NONE)
        session_start();

Básicamente, para iniciar la sesión si no la teniamos iniciada ya.

Y luego en la parte que resuelve las captchas:

    if (session_status() == PHP_SESSION_NONE)
        session_start();

Volvemos a tener estas 2 líneas.

	$secret = @$_SESSION["phrase"];

Obtenemos la solución de la sesión (si todo ha ido bien ese valor deería no ser nulo).

Esta parte `echo md5($input) === $secret;` ha cambiado por esta `echo $input === $secret;`.

Y con todo esto, ya tendremos asegurado un buen sistema captcha, antiguo y obsoleto, pero que nos brindará algo de seguridad.

Esto es el resultado (si hemos escrito bien el resultado):

![...](https://i.gyazo.com/a2bf8fbfff5fdaae1236645ce9155880.png)

Para el caso de que no sea válida:

![...](https://i.gyazo.com/1abc7bcb13c84e958d7031d416e6dc86.png)

Con ese resultado (`string`) casteado a `JObject` podremos determinar si hemos escrito bien o mal la captcha:

    JObject obj = JsonConvert.DeserializeObject<JObject>(Helpers.SolveCaptcha());

    try
    {
        Console.WriteLine("Has escrito {0} la captcha", (bool)obj["data"]["valid"] ? "BIEN" : "MAL");
    }
    catch
    {
        Console.WriteLine("Ha habido un error por parte del servidor: {0}", obj.ToString());
    }

![...](https://i.gyazo.com/a8d89daab728fc417f5c2e5aec510bcd.png)