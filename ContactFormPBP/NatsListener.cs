using System;
using System.Configuration;
using System.IO;
using System.Text;
using NATS.Client;

namespace ContactFormPBP
{
    public class NatsListener
    {
        private IConnection connection;

        private readonly ContactoRepository _contactoRepository;
        private readonly MailchimpService _mailchimpService;
        private readonly string _rutaLog;


        public NatsListener(
            ContactoRepository contactoRepository,
            MailchimpService mailchimpService,
            string rutaLog)
        {
            _contactoRepository = contactoRepository;
            _mailchimpService = mailchimpService;
            _rutaLog = rutaLog;
        }


        // ============================================================
        // START
        // ============================================================

        public void Start()
        {
            try
            {
                string natsUrl =
                    ConfigurationManager.AppSettings["NatsUrl"];

                string subject =
                    ConfigurationManager.AppSettings["NatsSubject"];


                Console.WriteLine("");
                Console.WriteLine("========================================");
                Console.WriteLine("INTENTANDO CONECTAR A NATS");
                Console.WriteLine("========================================");
                Console.WriteLine("Servidor : " + natsUrl);
                Console.WriteLine("Subject  : " + subject);
                Console.WriteLine("");


                Options options =
                    ConnectionFactory.GetDefaultOptions();

                options.Url = natsUrl;


                ConnectionFactory factory =
                    new ConnectionFactory();


                connection =
                    factory.CreateConnection(options);


                Console.WriteLine("");
                Console.WriteLine("========================================");
                Console.WriteLine("NATS CONECTADO");
                Console.WriteLine("========================================");
                Console.WriteLine("Servidor : " + natsUrl);
                Console.WriteLine("Subject  : " + subject);
                Console.WriteLine("");
                Console.WriteLine("Esperando mensajes...");
                Console.WriteLine("");


                EscribirLog(
                    "NATS conectado. Subject: " + subject
                );


                connection.SubscribeAsync(
                    subject,
                    (sender, args) =>
                    {
                        ProcesarMensaje(args);
                    }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine("");
                Console.WriteLine("========================================");
                Console.WriteLine("ERROR CONECTANDO A NATS");
                Console.WriteLine("========================================");

                Console.WriteLine("");
                Console.WriteLine("Tipo de error:");
                Console.WriteLine(ex.GetType().FullName);

                Console.WriteLine("");
                Console.WriteLine("Mensaje:");
                Console.WriteLine(ex.Message);

                Console.WriteLine("");
                Console.WriteLine("Detalle completo:");
                Console.WriteLine(ex.ToString());

                Console.WriteLine("");
                Console.WriteLine("========================================");


                EscribirLog(
                    "ERROR CONECTANDO A NATS: " + ex
                );
            }
        }


        // ============================================================
        // PROCESAR MENSAJE
        // ============================================================

        private void ProcesarMensaje(
            MsgHandlerEventArgs args)
        {
            try
            {
                string mensaje =
                    Encoding.UTF8.GetString(
                        args.Message.Data
                    );


                Console.WriteLine("");
                Console.WriteLine("========================================");
                Console.WriteLine("MENSAJE NATS RECIBIDO");
                Console.WriteLine("========================================");

                Console.WriteLine(mensaje);

                Console.WriteLine("");


                EscribirLog(
                    "Mensaje NATS recibido: " + mensaje
                );


                // ====================================================
                // DESERIALIZAR JSON
                // ====================================================

                NatsContact contacto =
                    Newtonsoft.Json.JsonConvert
                    .DeserializeObject<NatsContact>(
                        mensaje
                    );


                if (contacto == null)
                {
                    throw new Exception(
                        "El mensaje NATS no pudo deserializarse."
                    );
                }


                // ====================================================
                // MOSTRAR DATOS
                // ====================================================

                Console.WriteLine(
                    "CNAME : " + contacto.CNAME
                );

                Console.WriteLine(
                    "FNAME : " + contacto.FNAME
                );

                Console.WriteLine(
                    "LNAME : " + contacto.LNAME
                );

                Console.WriteLine(
                    "EMAIL : " + contacto.EMAIL
                );

                Console.WriteLine(
                    "PHONE : " + contacto.PHONE
                );

                Console.WriteLine(
                    "ACTI  : " + contacto.ACTI
                );


                // ====================================================
                // VALIDAR DATOS
                // ====================================================

                if (string.IsNullOrWhiteSpace(contacto.CNAME))
                    throw new Exception(
                        "CNAME vacío."
                    );

                if (string.IsNullOrWhiteSpace(contacto.FNAME))
                    throw new Exception(
                        "FNAME vacío."
                    );

                if (string.IsNullOrWhiteSpace(contacto.LNAME))
                    throw new Exception(
                        "LNAME vacío."
                    );

                if (string.IsNullOrWhiteSpace(contacto.EMAIL))
                    throw new Exception(
                        "EMAIL vacío."
                    );

                if (string.IsNullOrWhiteSpace(contacto.PHONE))
                    throw new Exception(
                        "PHONE vacío."
                    );

                if (string.IsNullOrWhiteSpace(contacto.ACTI))
                    throw new Exception(
                        "ACTI vacío."
                    );


                // ====================================================
                // VARIABLES TRIAL
                // ====================================================

                string trialUserName;
                string trialPassword;


                // ====================================================
                // CREAR CONTACTO EN SQL
                // ====================================================

                Console.WriteLine("");
                Console.WriteLine(
                    "========================================"
                );

                Console.WriteLine(
                    "CREANDO CONTACTO EN SQL"
                );

                Console.WriteLine(
                    "========================================"
                );


                bool contactoCreado =
                    _contactoRepository.CrearContacto(
                        contacto.CNAME,
                        contacto.FNAME,
                        contacto.LNAME,
                        contacto.EMAIL,
                        contacto.PHONE,
                        contacto.ACTI,
                        out int noInterno,
                        out string origen,
                        out trialUserName,
                        out trialPassword,
                        out string errorSql
                    );


                if (!contactoCreado)
                {
                    Console.WriteLine(
                        "ERROR CREANDO CONTACTO EN SQL:"
                    );

                    Console.WriteLine(
                        errorSql
                    );


                    EscribirLog(
                        "ERROR SQL. Email: "
                        + contacto.EMAIL
                        + " - "
                        + errorSql
                    );

                    return;
                }


                Console.WriteLine(
                    "Contacto creado correctamente."
                );

                Console.WriteLine(
                    "NoInterno: " + noInterno
                );

                Console.WriteLine(
                    "Origen: " + origen
                );



                // ====================================================
                // MOSTRAR DATOS TRIAL
                // ====================================================

                Console.WriteLine("");
                Console.WriteLine(
                    "DATOS TRIAL GENERADOS"
                );

                Console.WriteLine(
                    "Origen          : "
                    + origen
                );

                Console.WriteLine(
                    "Trial User Name : "
                    + trialUserName
                );

                Console.WriteLine(
                    "Trial Password  : "
                    + trialPassword
                );


                EscribirLog(
                    "Contacto creado en SQL. "
                    + "NoInterno: "
                    + noInterno
                    + " - Email: "
                    + contacto.EMAIL
                    + " - TrialUserName: "
                    + trialUserName
                );


                // ====================================================
                // ENVIAR A MAILCHIMP
                // ====================================================

                Console.WriteLine("");
                Console.WriteLine(
                    "========================================"
                );

                Console.WriteLine(
                    "ENVIANDO CONTACTO A MAILCHIMP"
                );

                Console.WriteLine(
                    "========================================"
                );


                bool mailchimpOk =
                 _mailchimpService.EnviarContacto(
                     contacto.CNAME,
                     contacto.FNAME,
                     contacto.LNAME,
                     contacto.EMAIL,
                     contacto.PHONE,
                     contacto.ACTI,
                     origen,
                     trialUserName,
                     trialPassword,
                     out string errorMailchimp
                 );



                if (!mailchimpOk)
                {
                    Console.WriteLine(
                        "ERROR ENVIANDO A MAILCHIMP:"
                    );

                    Console.WriteLine(
                        errorMailchimp
                    );


                    EscribirLog(
                        "ERROR MAILCHIMP. "
                        + "NoInterno: "
                        + noInterno
                        + " - Email: "
                        + contacto.EMAIL
                        + " - "
                        + errorMailchimp
                    );

                    return;
                }


                // ====================================================
                // CONTACTO PROCESADO CORRECTAMENTE
                // ====================================================

                Console.WriteLine("");
                Console.WriteLine(
                    "========================================"
                );

                Console.WriteLine(
                    "CONTACTO PROCESADO CORRECTAMENTE"
                );

                Console.WriteLine(
                    "========================================"
                );

                Console.WriteLine(
                    "NoInterno       : " + noInterno
                );

                Console.WriteLine(
                    "Email           : " + contacto.EMAIL
                );

                Console.WriteLine(
                    "Trial User Name : " + trialUserName
                );

                Console.WriteLine(
                    "Trial Password  : " + trialPassword
                );

                Console.WriteLine("");


                EscribirLog(
                    "CONTACTO PROCESADO CORRECTAMENTE. "
                    + "NoInterno: "
                    + noInterno
                    + " - Email: "
                    + contacto.EMAIL
                    + " - TrialUserName: "
                    + trialUserName
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine("");
                Console.WriteLine(
                    "========================================"
                );

                Console.WriteLine(
                    "ERROR PROCESANDO MENSAJE NATS"
                );

                Console.WriteLine(
                    "========================================"
                );

                Console.WriteLine(
                    ex.ToString()
                );

                Console.WriteLine("");


                EscribirLog(
                    "ERROR PROCESANDO MENSAJE NATS: "
                    + ex
                );
            }
        }


        // ============================================================
        // STOP
        // ============================================================

        public void Stop()
        {
            if (connection != null)
            {
                connection.Close();
                connection = null;
            }

            EscribirLog(
                "NATS desconectado."
            );
        }


        // ============================================================
        // LOG
        // ============================================================

        private void EscribirLog(
            string texto)
        {
            try
            {
                using (StreamWriter log =
                    new StreamWriter(
                        _rutaLog,
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
            catch
            {
                // El error del log no debe detener
                // el procesamiento del contacto.
            }
        }
    }


}