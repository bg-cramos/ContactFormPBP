using System;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace ContactFormPBP
{
    public class MailchimpService
    {
        private readonly string _apiKey;
        private readonly string _listId;

        private const string Tag = "Contact_Form_PBP";

        public MailchimpService()
        {
            _apiKey =
                ConfigurationManager.AppSettings["MailchimpApiKey"];

            _listId =
                ConfigurationManager.AppSettings["MailchimpListId"];

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new Exception(
                    "No se encontró MailchimpApiKey en AppSettings."
                );
            }

            if (string.IsNullOrWhiteSpace(_listId))
            {
                throw new Exception(
                    "No se encontró MailchimpListId en AppSettings."
                );
            }
        }


        // ========================================
        // ENVIAR CONTACTO A MAILCHIMP
        // ========================================

        public bool EnviarContacto(
            string cname,
            string fname,
            string lname,
            string email,
            string phone,
            string acti,
            string origen,
            string trialUserName,
            string trialPassword,
            out string error)
        {
            error = "";

            try
            {
                // ========================================
                // VALIDAR EMAIL
                // ========================================

                if (string.IsNullOrWhiteSpace(email))
                {
                    error = "EMAIL vacío.";
                    return false;
                }


                // ========================================
                // TODOS LOS DATOS COMO TEXTO
                // ========================================

                cname = cname ?? "";
                fname = fname ?? "";
                lname = lname ?? "";
                email = email.Trim();
                phone = phone ?? "";
                acti = acti ?? "";
                origen = origen ?? "";
                trialUserName = trialUserName ?? "";
                trialPassword = trialPassword ?? "";


                // ========================================
                // MD5 DEL EMAIL
                // ========================================

                string subscriberHash =
                    ObtenerHashEmail(email);


                // ========================================
                // CLIENTE HTTP
                // ========================================

                using (HttpClient client = new HttpClient())
                {
                    string auth =
                        Convert.ToBase64String(
                            Encoding.ASCII.GetBytes(
                                "anyuser:" + _apiKey
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
                        + _listId
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
                        + "\"PHONE\":\""
                        + EscapeJson(phone)
                        + "\","
                        + "\"ACTI\":\""
                        + EscapeJson(acti)
                        + "\","
                        + "\"ORCT\":\""
                        + EscapeJson(origen)
                        + "\","
                        + "\"TUSRNAM\":\""
                        + EscapeJson(trialUserName)
                        + "\","
                        + "\"TPASSWORD\":\""
                        + EscapeJson(trialPassword)
                        + "\""
                        + "}"
                        + "}";


                    // ========================================
                    // LOG DATOS
                    // ========================================

                    Console.WriteLine("");
                    Console.WriteLine(
                        "========================================"
                    );

                    Console.WriteLine(
                        "DATOS A ENVIAR A MAILCHIMP"
                    );

                    Console.WriteLine(
                        "========================================"
                    );

                    Console.WriteLine(
                        "Email        : " + email
                    );

                    Console.WriteLine(
                        "FNAME        : " + fname
                    );

                    Console.WriteLine(
                        "LNAME        : " + lname
                    );

                    Console.WriteLine(
                        "CNAME        : " + cname
                    );

                    Console.WriteLine(
                        "PHONE        : " + phone
                    );

                    Console.WriteLine(
                        "ACTI         : " + acti
                    );

                    Console.WriteLine(
                        "TUSRNAM      : " + trialUserName
                    );

                    Console.WriteLine(
                        "ORIGEN       : " + origen
                    );


                    Console.WriteLine(
                        "TPASSWORD    : " + trialPassword
                    );

                    Console.WriteLine(
                        "Tag          : " + Tag
                    );

                    Console.WriteLine(
                        "----------------------------------------"
                    );

                    Console.WriteLine(
                        "JSON:"
                    );

                    Console.WriteLine(
                        json
                    );

                    Console.WriteLine(
                        "----------------------------------------"
                    );


                    // ========================================
                    // PUT CONTACTO
                    // ========================================

                    using (HttpContent contenido =
                        new StringContent(
                            json,
                            Encoding.UTF8,
                            "application/json"))
                    {
                        Console.WriteLine(
                            "Actualizando contacto..."
                        );

                        HttpResponseMessage respuesta =
                            client.PutAsync(
                                url,
                                contenido
                            ).Result;


                        Console.WriteLine(
                            "Actualizar contacto: "
                            + respuesta.StatusCode
                        );


                        string respuestaTexto =
                            respuesta.Content
                            .ReadAsStringAsync()
                            .Result;


                        Console.WriteLine(
                            "Respuesta Mailchimp:"
                        );

                        Console.WriteLine(
                            respuestaTexto
                        );


                        if (!respuesta.IsSuccessStatusCode)
                        {
                            error =
                                "ERROR actualizando contacto: "
                                + respuesta.StatusCode
                                + " - "
                                + respuestaTexto;

                            return false;
                        }
                    }


                    // ========================================
                    // ELIMINAR TAG EXISTENTE
                    // ========================================

                    string tagUrl =
                        "https://us3.api.mailchimp.com/3.0/lists/"
                        + _listId
                        + "/members/"
                        + subscriberHash
                        + "/tags/"
                        + Uri.EscapeDataString(Tag);


                    Console.WriteLine("");
                    Console.WriteLine(
                        "Verificando etiqueta "
                        + Tag
                        + "..."
                    );


                    HttpResponseMessage quitarRespuesta =
                        client.DeleteAsync(
                            tagUrl
                        ).Result;


                    Console.WriteLine(
                        "Eliminar "
                        + Tag
                        + ": "
                        + quitarRespuesta.StatusCode
                    );


                    if (
                        quitarRespuesta.StatusCode !=
                        HttpStatusCode.NotFound
                        &&
                        !quitarRespuesta.IsSuccessStatusCode
                    )
                    {
                        string errorTag =
                            quitarRespuesta.Content
                            .ReadAsStringAsync()
                            .Result;

                        Console.WriteLine(
                            "ERROR eliminando "
                            + Tag
                            + ":"
                        );

                        Console.WriteLine(
                            errorTag
                        );

                        // No detenemos el proceso.
                        // Continuamos intentando agregar el tag.
                    }


                    // ========================================
                    // AGREGAR TAG
                    // ========================================

                    Console.WriteLine("");
                    Console.WriteLine(
                        "Agregando etiqueta "
                        + Tag
                        + "..."
                    );


                    string tagsUrl =
                        "https://us3.api.mailchimp.com/3.0/lists/"
                        + _listId
                        + "/members/"
                        + subscriberHash
                        + "/tags";


                    string agregarTagJson =
                        "{"
                        + "\"tags\":["
                        + "{"
                        + "\"name\":\""
                        + EscapeJson(Tag)
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
                            + Tag
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


                        if (!agregarRespuesta.IsSuccessStatusCode)
                        {
                            error =
                                "ERROR agregando "
                                + Tag
                                + ": "
                                + agregarTexto;

                            return false;
                        }
                    }
                }


                // ========================================
                // TODO OK
                // ========================================

                Console.WriteLine("");
                Console.WriteLine(
                    "========================================"
                );

                Console.WriteLine(
                    "CONTACTO ENVIADO CORRECTAMENTE A MAILCHIMP"
                );

                Console.WriteLine(
                    "========================================"
                );


                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("");
                Console.WriteLine(
                    "========================================"
                );

                Console.WriteLine(
                    "ERROR EN MAILCHIMP"
                );

                Console.WriteLine(
                    "========================================"
                );

                Console.WriteLine(
                    ex.ToString()
                );


                error = ex.ToString();

                return false;
            }
        }


        // ========================================
        // MD5 DEL EMAIL
        // ========================================

        private string ObtenerHashEmail(
            string email)
        {
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

                return sb.ToString();
            }
        }


        // ========================================
        // ESCAPAR JSON
        // ========================================

        private string EscapeJson(
            string texto)
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