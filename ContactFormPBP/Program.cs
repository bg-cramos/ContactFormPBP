using System;
using System.Data.SqlClient;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http;
using System.IO;
using System.Configuration;

namespace ContactFormPBP
{
    class Program
    {
        static void Main(string[] args)
        {
            string rutaLog = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "MailchimpContactFormPBP_Log.txt"
            );

            // ========================================
            // INICIO DEL LOG
            // ========================================

            using (StreamWriter log = new StreamWriter(
                rutaLog,
                false,
                Encoding.UTF8))
            {
                log.WriteLine("========================================");
                log.WriteLine("MAILCHIMP CONTACT_FORM_PBP - LOG");
                log.WriteLine(
                    "Inicio: " +
                    DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                log.WriteLine("========================================");
            }

            ServicePointManager.Expect100Continue = true;

            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls12 |
                SecurityProtocolType.Tls11 |
                SecurityProtocolType.Tls;

            // ========================================
            // CONFIGURACION DE SQL
            // ========================================

            string servidor =
                ConfigurationManager.AppSettings["SqlServer"];

            string baseDatos =
                ConfigurationManager.AppSettings["SqlDatabase"];

            string usuario =
                ConfigurationManager.AppSettings["SqlUser"];

            string password =
                ConfigurationManager.AppSettings["SqlPassword"];

            string connectionString =
                "Server=" + servidor +
                ";Database=" + baseDatos +
                ";User Id=" + usuario +
                ";Password=" + password + ";";


            // ========================================
            // CONFIGURACION DE MAILCHIMP
            // ========================================

            var apiKey = ConfigurationManager.AppSettings["MailchimpApiKey"];

            string listId = "ede57d027d";

            // ========================================
            // ETIQUETA
            // ========================================

            const string tag = "Contact_Form_PBP";

            // ========================================
            // CONSULTA SQL
            // ========================================

            string query = @"
                SELECT 
                    MKC.RazonSocial AS CNAME,
                    MKCO.Nombres AS FNAME,
                    MKCO.Apellido AS LNAME,
                    EMAIL.Descripcion AS EMAIL,
                    PHONE.Descripcion AS PHONE,
                    ORIGEN.Contenido AS ORCT,
                    ACTIVIDAD.Contenido AS ACTI

                FROM Mk_Cuentas MKC

                INNER JOIN Mk_Contactos MKCO
                    ON MKC.NoInterno = MKCO.NoInterno
                    AND MKCO.ContactoPrincipal = 1

                INNER JOIN Mk_CanalComunicacionCuentas EMAIL
                    ON EMAIL.NoInterno = MKC.NoInterno
                    AND EMAIL.CoMedioComunicacion = 1
                    AND EMAIL.Principal = 1

                INNER JOIN Mk_CanalComunicacionCuentas PHONE
                    ON PHONE.NoInterno = MKC.NoInterno
                    AND PHONE.CoMedioComunicacion = 0
                    AND PHONE.Principal = 1

                INNER JOIN Mk_Parametros_Cuentas  ACTIVIDAD
                    ON ACTIVIDAD.NoInterno = MKC.NoInterno
                    AND ACTIVIDAD.CoParametro = 3

                INNER JOIN Mk_Parametros_Cuentas ORIGEN
                    ON ORIGEN.NoInterno = MKC.NoInterno
                    AND ORIGEN.CoParametro = 6
                    AND ORIGEN.NoElemento = 15

            ";

            try
            {
                using (SqlConnection conexion =
                    new SqlConnection(connectionString))
                {
                    conexion.Open();

                    Console.WriteLine(
                        "========================================");

                    Console.WriteLine(
                        "CONEXION SQL OK");

                    Console.WriteLine(
                        "========================================");

                    Console.WriteLine("");

                    EscribirLog(
                        rutaLog,
                        "Conexion SQL OK."
                    );

                    // ========================================
                    // EJECUTAR CONSULTA
                    // ========================================

                    using (SqlCommand comando =
                        new SqlCommand(query, conexion))
                    {
                        using (SqlDataReader reader =
                            comando.ExecuteReader())
                        {
                            int cantidad = 0;

                            while (reader.Read())
                            {
                                cantidad++;

                                // ========================================
                                // DATOS DEL CONTACTO
                                // ========================================

                                string fname =
                                    reader["FNAME"].ToString();

                                string lname =
                                    reader["LNAME"].ToString();

                                string email =
                                    reader["EMAIL"]
                                    .ToString()
                                    .Trim();

                                string phone =
                                    reader["PHONE"]
                                    .ToString()
                                    .Trim();

                                string cname =
                                    reader["CNAME"].ToString();

                                string orct =
                                    reader["ORCT"].ToString();

                                string acti =
                                    reader["ACTI"].ToString();

                                Console.WriteLine("");
                                Console.WriteLine(
                                    "----------------------------------------");

                                Console.WriteLine(
                                    "Procesando contacto #" +
                                    cantidad);

                                Console.WriteLine(
                                    "----------------------------------------");

                                Console.WriteLine(
                                    "Email      : " + email);

                                Console.WriteLine(
                                    "Telefono   : " + phone);

                                Console.WriteLine(
                                    "Empresa    : " + cname);

                                Console.WriteLine(
                                    "Origen     : " + orct);

                                Console.WriteLine(
                                    "Actividad  : " + acti);

                                Console.WriteLine(
                                    "Tag        : " + tag);

                                EscribirLog(
                                    rutaLog,
                                    "Procesando contacto #" +
                                    cantidad +
                                    " - Email: " +
                                    email +
                                    " - Empresa: " +
                                    cname
                                );

                                // ========================================
                                // VALIDAR EMAIL
                                // ========================================

                                if (string.IsNullOrWhiteSpace(email))
                                {
                                    Console.WriteLine(
                                        "ERROR: Email vacio."
                                    );

                                    EscribirLog(
                                        rutaLog,
                                        "ERROR: Email vacio."
                                    );

                                    continue;
                                }

                                // ========================================
                                // MD5 DEL EMAIL
                                // ========================================

                                string subscriberHash;

                                using (MD5 md5 = MD5.Create())
                                {
                                    byte[] bytes =
                                        Encoding.UTF8.GetBytes(
                                            email.ToLowerInvariant()
                                        );

                                    byte[] hash =
                                        md5.ComputeHash(bytes);

                                    StringBuilder sb =
                                        new StringBuilder();

                                    foreach (byte b in hash)
                                    {
                                        sb.Append(
                                            b.ToString("x2")
                                        );
                                    }

                                    subscriberHash =
                                        sb.ToString();
                                }

                                // ========================================
                                // CLIENTE HTTP
                                // ========================================

                                using (HttpClient client =
                                    new HttpClient())
                                {
                                    string auth =
                                        Convert.ToBase64String(
                                            Encoding.ASCII.GetBytes(
                                                "anyuser:" + apiKey
                                            )
                                        );

                                    client.DefaultRequestHeaders.Add(
                                        "Authorization",
                                        "Basic " + auth
                                    );

                                    // ========================================
                                    // URL DEL CONTACTO
                                    // ========================================

                                    string url =
                                        "https://us3.api.mailchimp.com/3.0/lists/"
                                        + listId
                                        + "/members/"
                                        + subscriberHash;

                                    // ========================================
                                    // JSON
                                    // ========================================

                                    string json =
                                        "{"
                                        + "\"email_address\":\""
                                        + EscapeJson(email)
                                        + "\","
                                        + "\"status_if_new\":\"subscribed\","
                                        + "\"merge_fields\":{"
                                        + "\"FNAME\":\""
                                        + EscapeJson(fname)
                                        + "\","
                                        + "\"LNAME\":\""
                                        + EscapeJson(lname)
                                        + "\","
                                        + "\"CNAME\":\""
                                        + EscapeJson(cname)
                                        + "\","
                                        + "\"ORCT\":\""
                                        + EscapeJson(orct)
                                        + "\","
                                        + "\"ACTI\":\""
                                        + EscapeJson(acti)
                                        + "\""
                                        + "}"
                                        + "}";

                                    using (HttpContent contenido =
                                        new StringContent(
                                            json,
                                            Encoding.UTF8,
                                            "application/json"))
                                    {
                                        Console.WriteLine("");
                                        Console.WriteLine(
                                            "========================================");

                                        Console.WriteLine(
                                            "DATOS A ENVIAR A MAILCHIMP");

                                        Console.WriteLine(
                                            "========================================");

                                        Console.WriteLine(
                                            "Contacto #" +
                                            cantidad);

                                        Console.WriteLine(
                                            "Email      : " +
                                            email);

                                        Console.WriteLine(
                                            "FNAME      : " +
                                            fname);

                                        Console.WriteLine(
                                            "LNAME      : " +
                                            lname);

                                        Console.WriteLine(
                                            "CNAME      : " +
                                            cname);

                                        Console.WriteLine(
                                            "ORCT       : " +
                                            orct);

                                        Console.WriteLine(
                                            "ACTI       : " +
                                            acti);

                                        Console.WriteLine(
                                            "Tag        : " +
                                            tag);

                                        Console.WriteLine(
                                            "----------------------------------------");

                                        Console.WriteLine(
                                            "Actualizando datos del contacto..."
                                        );

                                        // ========================================
                                        // PUT CONTACTO
                                        // ========================================

                                        HttpResponseMessage respuesta =
                                            client.PutAsync(
                                                url,
                                                contenido
                                            ).Result;

                                        Console.WriteLine(
                                            "Actualizar contacto: "
                                            + respuesta.StatusCode
                                        );

                                        if (!respuesta.IsSuccessStatusCode)
                                        {
                                            string errorTexto =
                                                respuesta.Content
                                                .ReadAsStringAsync()
                                                .Result;

                                            Console.WriteLine(
                                                "ERROR ACTUALIZANDO CONTACTO:"
                                            );

                                            Console.WriteLine(
                                                errorTexto
                                            );

                                            EscribirLog(
                                                rutaLog,
                                                "ERROR actualizando contacto "
                                                + email
                                                + ": "
                                                + errorTexto
                                            );

                                            continue;
                                        }

                                        EscribirLog(
                                            rutaLog,
                                            "Contacto actualizado correctamente: "
                                            + email
                                        );
                                    }

                                    // ========================================
                                    // ELIMINAR ETIQUETA SI EXISTE
                                    // ========================================

                                    Console.WriteLine("");

                                    Console.WriteLine(
                                        "Verificando etiqueta "
                                        + tag
                                        + "..."
                                    );

                                    string tagUrl =
                                        "https://us3.api.mailchimp.com/3.0/lists/"
                                        + listId
                                        + "/members/"
                                        + subscriberHash
                                        + "/tags/"
                                        + Uri.EscapeDataString(tag);

                                    HttpResponseMessage quitarRespuesta =
                                        client.DeleteAsync(
                                            tagUrl
                                        ).Result;

                                    Console.WriteLine(
                                        "Eliminar "
                                        + tag
                                        + ": "
                                        + quitarRespuesta.StatusCode
                                    );

                                    // ========================================
                                    // RESULTADO ELIMINAR TAG
                                    // ========================================

                                    if (
                                        quitarRespuesta.StatusCode ==
                                        HttpStatusCode.NotFound
                                    )
                                    {
                                        Console.WriteLine(
                                            tag
                                            + " no existia."
                                        );

                                        EscribirLog(
                                            rutaLog,
                                            tag
                                            + " no existia para "
                                            + email
                                        );
                                    }
                                    else if (
                                        quitarRespuesta.IsSuccessStatusCode
                                    )
                                    {
                                        Console.WriteLine(
                                            tag
                                            + " eliminada correctamente."
                                        );

                                        EscribirLog(
                                            rutaLog,
                                            tag
                                            + " eliminada para "
                                            + email
                                        );
                                    }
                                    else
                                    {
                                        string errorTag =
                                            quitarRespuesta.Content
                                            .ReadAsStringAsync()
                                            .Result;

                                        Console.WriteLine(
                                            "ERROR ELIMINANDO "
                                            + tag
                                            + ":"
                                        );

                                        Console.WriteLine(
                                            errorTag
                                        );

                                        EscribirLog(
                                            rutaLog,
                                            "ERROR eliminando "
                                            + tag
                                            + " para "
                                            + email
                                            + ": "
                                            + errorTag
                                        );
                                    }

                                    // ========================================
                                    // AGREGAR ETIQUETA
                                    // ========================================

                                    Console.WriteLine("");

                                    Console.WriteLine(
                                        "Agregando etiqueta "
                                        + tag
                                        + "..."
                                    );

                                    string tagsUrl =
                                        "https://us3.api.mailchimp.com/3.0/lists/"
                                        + listId
                                        + "/members/"
                                        + subscriberHash
                                        + "/tags";

                                    string agregarTagJson =
                                        "{"
                                        + "\"tags\":["
                                        + "{"
                                        + "\"name\":\""
                                        + EscapeJson(tag)
                                        + "\","
                                        + "\"status\":\"active\""
                                        + "}"
                                        + "]"
                                        + "}";

                                    using (HttpContent agregarContenido =
                                        new StringContent(
                                            agregarTagJson,
                                            Encoding.UTF8,
                                            "application/json"))
                                    {
                                        HttpResponseMessage agregarRespuesta =
                                            client.PostAsync(
                                                tagsUrl,
                                                agregarContenido
                                            ).Result;

                                        Console.WriteLine(
                                            "Agregar "
                                            + tag
                                            + ": "
                                            + agregarRespuesta.StatusCode
                                        );

                                        string agregarTexto =
                                            agregarRespuesta.Content
                                            .ReadAsStringAsync()
                                            .Result;

                                        Console.WriteLine(
                                            agregarTexto
                                        );

                                        if (
                                            agregarRespuesta
                                            .IsSuccessStatusCode
                                        )
                                        {
                                            Console.WriteLine("");

                                            Console.WriteLine(
                                                "Contacto procesado correctamente."
                                            );

                                            EscribirLog(
                                                rutaLog,
                                                "Contacto procesado correctamente: "
                                                + email
                                                + " - "
                                                + tag
                                                + " agregada."
                                            );
                                        }
                                        else
                                        {
                                            Console.WriteLine("");

                                            Console.WriteLine(
                                                "ERROR AGREGANDO "
                                                + tag
                                                + ":"
                                            );

                                            Console.WriteLine(
                                                agregarTexto
                                            );

                                            EscribirLog(
                                                rutaLog,
                                                "ERROR agregando "
                                                + tag
                                                + " para "
                                                + email
                                                + ": "
                                                + agregarTexto
                                            );
                                        }
                                    }
                                }
                            }

                            // ========================================
                            // TOTAL
                            // ========================================

                            Console.WriteLine("");

                            Console.WriteLine(
                                "========================================"
                            );

                            Console.WriteLine(
                                "TOTAL DE CONTACTOS PROCESADOS: "
                                + cantidad
                            );

                            Console.WriteLine(
                                "========================================"
                            );

                            EscribirLog(
                                rutaLog,
                                "TOTAL DE CONTACTOS PROCESADOS: "
                                + cantidad
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("");

                Console.WriteLine(
                    "========================================"
                );

                Console.WriteLine(
                    "ERROR:"
                );

                Console.WriteLine(
                    "========================================"
                );

                Console.WriteLine(
                    ex.ToString()
                );

                EscribirLog(
                    rutaLog,
                    "ERROR GENERAL: " + ex.ToString()
                );
            }

            Console.WriteLine("");

            Console.WriteLine(
                "Proceso terminado"
            );

            Console.ReadLine();

        }

        // ========================================
        // ESCRIBIR LOG
        // ========================================

        static void EscribirLog(
            string rutaLog,
            string texto)
        {
            using (StreamWriter log =
                new StreamWriter(
                    rutaLog,
                    true,
                    Encoding.UTF8))
            {
                log.WriteLine(
                    DateTime.Now.ToString(
                        "dd/MM/yyyy HH:mm:ss")
                    + " - "
                    + texto
                );
            }
        }

        // ========================================
        // ESCAPAR TEXTO PARA JSON
        // ========================================

        static string EscapeJson(string texto)
        {
            if (texto == null)
            {
                return "";
            }

            return texto
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }

    }

}
