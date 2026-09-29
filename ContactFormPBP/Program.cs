using System;
using System.Configuration;
using System.IO;
using System.Text;
using System.Diagnostics;


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

            try
            {
                RegistrarEventSource();

                InicializarLog(rutaLog);

                MostrarPaso(
                    rutaLog,
                    "INICIO DEL PROGRAMA"
                );

                MostrarPaso(
                    rutaLog,
                    "Directorio base: "
                    + AppDomain.CurrentDomain.BaseDirectory
                );

                MostrarPaso(
                    rutaLog,
                    "Verificando archivo AppSettings.local.config..."
                );

                string rutaAppSettings =
                    Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "AppSettings.local.config"
                    );

                if (File.Exists(rutaAppSettings))
                {
                    MostrarOK(
                        rutaLog,
                        "AppSettings.local.config encontrado."
                    );

                    MostrarPaso(
                        rutaLog,
                        "Ruta: " + rutaAppSettings
                    );
                }
                else
                {
                    MostrarError(
                        rutaLog,
                        "NO SE ENCONTRO AppSettings.local.config"
                    );

                    MostrarPaso(
                        rutaLog,
                        "Ruta esperada: " + rutaAppSettings
                    );

                    throw new Exception(
                        "No existe el archivo AppSettings.local.config."
                    );
                }

                MostrarPaso(
                    rutaLog,
                    "Leyendo configuracion..."
                );

                string servidor =
                    ConfigurationManager.AppSettings["SqlServer"];

                string baseDatos =
                    ConfigurationManager.AppSettings["SqlDatabase"];

                string usuario =
                    ConfigurationManager.AppSettings["SqlUser"];

                string password =
                    ConfigurationManager.AppSettings["SqlPassword"];

                string mailchimpApiKey =
                    ConfigurationManager.AppSettings["MailchimpApiKey"];

                string natsUrl =
                    ConfigurationManager.AppSettings["NatsUrl"];

                string natsSubject =
                    ConfigurationManager.AppSettings["NatsSubject"];


                // ========================================
                // VALIDACION SQL
                // ========================================

                MostrarPaso(
                    rutaLog,
                    "Validando configuracion SQL..."
                );

                ValidarConfiguracion(
                    "SqlServer",
                    servidor
                );

                ValidarConfiguracion(
                    "SqlDatabase",
                    baseDatos
                );

                ValidarConfiguracion(
                    "SqlUser",
                    usuario
                );

                ValidarConfiguracion(
                    "SqlPassword",
                    password
                );

                MostrarOK(
                    rutaLog,
                    "Configuracion SQL encontrada."
                );

                Console.WriteLine(
                    "Servidor : " + servidor
                );

                Console.WriteLine(
                    "Base     : " + baseDatos
                );

                Console.WriteLine(
                    "Usuario  : " + usuario
                );

                Console.WriteLine(
                    "Password : configurado"
                );


                // ========================================
                // VALIDACION MAILCHIMP
                // ========================================

                MostrarPaso(
                    rutaLog,
                    "Validando configuracion Mailchimp..."
                );

                ValidarConfiguracion(
                    "MailchimpApiKey",
                    mailchimpApiKey
                );

                MostrarOK(
                    rutaLog,
                    "MailchimpApiKey encontrada."
                );

                Console.WriteLine(
                    "API Key  : configurada"
                );


                // ========================================
                // VALIDACION NATS
                // ========================================

                MostrarPaso(
                    rutaLog,
                    "Validando configuracion NATS..."
                );

                ValidarConfiguracion(
                    "NatsUrl",
                    natsUrl
                );

                ValidarConfiguracion(
                    "NatsSubject",
                    natsSubject
                );

                MostrarOK(
                    rutaLog,
                    "Configuracion NATS encontrada."
                );

                Console.WriteLine(
                    "Servidor : " + natsUrl
                );

                Console.WriteLine(
                    "Subject  : " + natsSubject
                );

                Console.WriteLine(
                    "Password : configurado"
                );


                // ========================================
                // CONNECTION STRING SQL
                // ========================================

                MostrarPaso(
                    rutaLog,
                    "Construyendo connection string SQL..."
                );

                string connectionString =
                    "Server=" + servidor +
                    ";Database=" + baseDatos +
                    ";User Id=" + usuario +
                    ";Password=" + password + ";";

                MostrarOK(
                    rutaLog,
                    "Connection string SQL creado."
                );


                // ========================================
                // CONTACTO REPOSITORY
                // ========================================

                MostrarPaso(
                    rutaLog,
                    "Creando ContactoRepository..."
                );

                ContactoRepository contactoRepository =
                    new ContactoRepository(
                        connectionString
                    );

                MostrarOK(
                    rutaLog,
                    "ContactoRepository creado correctamente."
                );


                // ========================================
                // MAILCHIMP SERVICE
                // ========================================

                MostrarPaso(
                    rutaLog,
                    "Creando MailchimpService..."
                );

                MailchimpService mailchimpService =
                    new MailchimpService();

                MostrarOK(
                    rutaLog,
                    "MailchimpService creado correctamente."
                );


                // ========================================
                // NATS LISTENER
                // ========================================

                MostrarPaso(
                    rutaLog,
                    "Creando NatsListener..."
                );

                NatsListener natsListener =
                    new NatsListener(
                        contactoRepository,
                        mailchimpService,
                        rutaLog
                    );

                MostrarOK(
                    rutaLog,
                    "NatsListener creado correctamente."
                );


                // ========================================
                // INICIAR NATS
                // ========================================

                MostrarPaso(
                    rutaLog,
                    "Iniciando conexion NATS..."
                );

                natsListener.Start();

                MostrarOK(
                    rutaLog,
                    "NatsListener.Start() ejecutado."
                );


                // ========================================
                // SISTEMA INICIADO
                // ========================================

                Console.WriteLine("");

                Console.WriteLine(
                    "========================================"
                );

                Console.WriteLine(
                    "SISTEMA INICIADO CORRECTAMENTE"
                );

                Console.WriteLine(
                    "========================================"
                );

                Console.WriteLine("");

                Console.WriteLine(
                    "NATS       : conectado"
                );

                Console.WriteLine(
                    "Subject    : " + natsSubject
                );

                Console.WriteLine(
                    "SQL        : configurado"
                );

                Console.WriteLine(
                    "Mailchimp  : configurado"
                );

                Console.WriteLine("");

                Console.WriteLine(
                    "Esperando nuevos contactos..."
                );

                Console.WriteLine("");

                EscribirLog(
                     rutaLog,
                     "Sistema iniciado correctamente. Esperando mensajes NATS."
                 );

                EscribirEventoWindows(
                    "Sistema iniciado correctamente. Esperando mensajes NATS.",
                    EventLogEntryType.Information
                );


                // ========================================
                // MANTENER EL PROCESO VIVO
                // ========================================
                //
                // No se utiliza Console.ReadLine()
                // porque el proceso sera ejecutado
                // posteriormente como servicio.
                //
                // El NatsListener queda ejecutandose
                // mientras este proceso permanezca vivo.
                //

                System.Threading.Thread.Sleep(
                    System.Threading.Timeout.Infinite
                );
            }
            catch (Exception ex)
            {
                // ========================================
                // ERROR GENERAL
                // ========================================

                Console.WriteLine("");

                Console.WriteLine(
                    "========================================"
                );

                Console.WriteLine(
                    "ERROR GENERAL"
                );

                Console.WriteLine(
                    "========================================"
                );

                Console.WriteLine("");

                Console.WriteLine(
                    "Tipo:"
                );

                Console.WriteLine(
                    ex.GetType().FullName
                );

                Console.WriteLine("");

                Console.WriteLine(
                    "Mensaje:"
                );

                Console.WriteLine(
                    ex.Message
                );

                Console.WriteLine("");

                Console.WriteLine(
                    "Detalle completo:"
                );

                Console.WriteLine(
                    ex.ToString()
                );

                Console.WriteLine("");


                // ========================================
                // INNER EXCEPTIONS
                // ========================================

                Exception inner =
                    ex.InnerException;

                while (inner != null)
                {
                    Console.WriteLine("");

                    Console.WriteLine(
                        "----------------------------------------"
                    );

                    Console.WriteLine(
                        "INNER EXCEPTION"
                    );

                    Console.WriteLine(
                        "----------------------------------------"
                    );

                    Console.WriteLine(
                        "Tipo:"
                    );

                    Console.WriteLine(
                        inner.GetType().FullName
                    );

                    Console.WriteLine("");

                    Console.WriteLine(
                        "Mensaje:"
                    );

                    Console.WriteLine(
                        inner.Message
                    );

                    EscribirLog(
                        rutaLog,
                        "INNER EXCEPTION: "
                        + inner.GetType().FullName
                        + " - "
                        + inner.Message
                    );

                    inner =
                        inner.InnerException;
                }


                // ========================================
                // LOG DEL ERROR
                // ========================================

                EscribirLog(
                    rutaLog,
                    "ERROR GENERAL: "
                    + ex.GetType().FullName
                    + " - "
                    + ex.Message
                );

                EscribirEventoWindows(
                    "ERROR GENERAL: "
                    + ex.GetType().FullName
                    + " - "
                    + ex.Message,
                    EventLogEntryType.Error
                );



                // ========================================
                // INFORMACION EN CONSOLA
                // ========================================

                Console.WriteLine("");

                Console.WriteLine(
                    "========================================"
                );

                Console.WriteLine(
                    "EL PROGRAMA SE DETENDRA POR ERROR"
                );

                Console.WriteLine(
                    "Revise el archivo de log:"
                );

                Console.WriteLine(
                    rutaLog
                );

                Console.WriteLine(
                    "========================================"
                );


                // ========================================
                // CODIGO DE ERROR
                // ========================================

                Environment.ExitCode = 1;
            }
        }


        // ============================================
        // INICIALIZAR LOG
        // ============================================

        static void InicializarLog(
            string rutaLog)
        {
            using (StreamWriter log =
                new StreamWriter(
                    rutaLog,
                    false,
                    Encoding.UTF8))
            {
                log.WriteLine(
                    "========================================"
                );

                log.WriteLine(
                    "CONTACT FORM PBP - LOG"
                );

                log.WriteLine(
                    "Inicio: "
                    + DateTime.Now.ToString(
                        "dd/MM/yyyy HH:mm:ss"
                    )
                );

                log.WriteLine(
                    "========================================"
                );
            }
        }


        // ============================================
        // MOSTRAR PASO
        // ============================================

        static void MostrarPaso(
            string rutaLog,
            string texto)
        {
            Console.WriteLine(
                "[PASO] " + texto
            );

            EscribirLog(
                rutaLog,
                "[PASO] " + texto
            );
        }


        // ============================================
        // MOSTRAR OK
        // ============================================

        static void MostrarOK(
            string rutaLog,
            string texto)
        {
            Console.WriteLine(
                "[OK] " + texto
            );

            EscribirLog(
                rutaLog,
                "[OK] " + texto
            );
        }


        // ============================================
        // MOSTRAR ERROR
        // ============================================

        static void MostrarError(
            string rutaLog,
            string texto)
        {
            Console.WriteLine(
                "[ERROR] " + texto
            );

            EscribirLog(
                rutaLog,
                "[ERROR] " + texto
            );
        }


        // ============================================
        // VALIDAR CONFIGURACION
        // ============================================

        static void ValidarConfiguracion(
            string nombre,
            string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                throw new ConfigurationErrorsException(
                    "Falta la configuracion: "
                    + nombre
                );
            }
        }


        // ============================================
        // ESCRIBIR LOG
        // ============================================

        public static void EscribirLog(
            string rutaLog,
            string texto)
        {
            try
            {
                using (StreamWriter log =
                    new StreamWriter(
                        rutaLog,
                        true,
                        Encoding.UTF8))
                {
                    log.WriteLine(
                        DateTime.Now.ToString(
                            "dd/MM/yyyy HH:mm:ss"
                        )
                        + " - "
                        + texto
                    );
                }
            }
            catch
            {
                // No detener el proceso
                // por un error de escritura del log.
            }
        }

        // ============================================
        // ESCRIBIR EVENTO EN WINDOWS
        // ============================================

        public static void EscribirEventoWindows(
            string texto,
            EventLogEntryType tipo)
        {
            try
            {
                EventLog.WriteEntry(
                    "ContactFormPBP",
                    texto,
                    tipo
                );
            }
            catch
            {
                // Un error del Event Viewer
                // no debe detener el servicio.
            }
        }

        // ============================================
        // REGISTRAR EVENT SOURCE EN WINDOWS
        // ============================================

        public static void RegistrarEventSource()
        {
            try
            {
                string sourceName = "ContactFormPBP";
                string logName = "Application";

                if (!EventLog.SourceExists(sourceName))
                {
                    EventLog.CreateEventSource(
                        sourceName,
                        logName
                    );
                }
            }
            catch
            {
                // Si no hay permisos para crear el origen,
                // no detener el programa.
            }
        }


    }
}
