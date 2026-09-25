using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace ContactFormPBP
{
    public class ContactoRepository
    {
        private readonly string _connectionString;

        // ============================================================
        // MAPPING ACTIVIDAD
        // ============================================================

        private static readonly Dictionary<string, string> ActividadMapping =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Ya existentes en Mk_Elementos
                { "Construcción", "017" },
                { "Salud", "015" },
                { "Servicios", "020" },
                { "Comercio Exterior", "209" },
                { "ONGs", "247" },
                { "Organismos Públicos", "248" },

                // Nuevos elementos
                { "Alimentos y Bebidas", "249" },
                { "Automotriz y Metalúrgicas", "250" },
                { "Comunicación y Gráfica", "251" },
                { "Otras Industrias", "252" },
                { "Plásticos", "179" },
                { "Seguridad", "039" },
                { "Textíl", "001" },
                { "Otros", "000" }
            };


        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public ContactoRepository(string connectionString)
        {
            _connectionString = connectionString;
        }


        // ============================================================
        // LOG
        // ============================================================

        private static void Log(string mensaje)
        {
            Console.WriteLine(
                "[" +
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") +
                "] " +
                mensaje);
        }


        // ============================================================
        // LOG DE PARAMETROS SQL
        // ============================================================

        private static void LogParametros(SqlCommand command)
        {
            foreach (SqlParameter p in command.Parameters)
            {
                string valor;

                if (p.Value == null)
                {
                    valor = "<null>";
                }
                else if (p.Value == DBNull.Value)
                {
                    valor = "<DBNull>";
                }
                else
                {
                    valor = p.Value.ToString();
                }

                Log(
                    "    " +
                    p.ParameterName +
                    " = [" +
                    valor +
                    "]" +
                    " | Type=" +
                    p.SqlDbType +
                    " | Direction=" +
                    p.Direction);
            }
        }


        // ============================================================
        // MÉTODO PRINCIPAL
        // ============================================================

        public bool CrearContacto(
            string cname,
            string fname,
            string lname,
            string email,
            string phone,
            string actividad,
            out int noInterno,
            out string origen,
            out string trialUserName,
            out string trialPassword,
            out string error)


        {
            noInterno = 0;
            origen = string.Empty;
            trialUserName = string.Empty;
            trialPassword = string.Empty;
            error = string.Empty;



            Log("========================================");
            Log("INICIO CrearContacto");
            Log("========================================");

            Log("CNAME     = [" + cname + "]");
            Log("FNAME     = [" + fname + "]");
            Log("LNAME     = [" + lname + "]");
            Log("EMAIL     = [" + email + "]");
            Log("PHONE     = [" + phone + "]");
            Log("ACTIVIDAD = [" + actividad + "]");


            // ========================================================
            // VALIDACIONES
            // ========================================================

            if (string.IsNullOrWhiteSpace(cname))
            {
                error = "CNAME es obligatorio.";
                Log("VALIDACION ERROR: " + error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(fname))
            {
                error = "FNAME es obligatorio.";
                Log("VALIDACION ERROR: " + error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(lname))
            {
                error = "LNAME es obligatorio.";
                Log("VALIDACION ERROR: " + error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                error = "EMAIL es obligatorio.";
                Log("VALIDACION ERROR: " + error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(phone))
            {
                error = "PHONE es obligatorio.";
                Log("VALIDACION ERROR: " + error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(actividad))
            {
                error = "ACTI es obligatorio.";
                Log("VALIDACION ERROR: " + error);
                return false;
            }


            // ========================================================
            // MAPPING ACTIVIDAD
            // ========================================================

            string noElementoActividad;

            if (!ActividadMapping.TryGetValue(
                    actividad.Trim(),
                    out noElementoActividad))
            {
                error =
                    "No existe mapping para la actividad: " +
                    actividad;

                Log("MAPPING ERROR: " + error);

                return false;
            }

            Log(
                "Actividad encontrada. " +
                "NoElemento = [" +
                noElementoActividad +
                "]");


            // ========================================================
            // CONEXION SQL
            // ========================================================

            using (SqlConnection connection =
                   new SqlConnection(_connectionString))
            {
                try
                {
                    Log("Abriendo conexión SQL...");

                    connection.Open();

                    Log(
                        "SQL CONECTADO: " +
                        connection.DataSource +
                        " / " +
                        connection.Database);
                }
                catch (SqlException ex)
                {
                    Log("========================================");
                    Log("ERROR ABRIENDO CONEXION SQL");
                    Log("========================================");

                    Log("Number    : " + ex.Number);
                    Log("Message   : " + ex.Message);
                    Log("Procedure : " + ex.Procedure);
                    Log("Line      : " + ex.LineNumber);
                    Log("StackTrace:");
                    Log(ex.ToString());

                    error = ex.ToString();

                    return false;
                }


                using (SqlTransaction transaction =
                       connection.BeginTransaction())
                {
                    try
                    {
                        // ====================================================
                        // PASO 1 - CREAR CUENTA
                        // ====================================================

                        Log("========================================");
                        Log("PASO 1 - CREAR CUENTA");
                        Log("========================================");

                        noInterno = CrearCuenta(
                            connection,
                            transaction,
                            cname,
                            out int errCuenta);

                        Log(
                            "PASO 1 FINALIZADO. " +
                            "NoInterno = " +
                            noInterno +
                            " | Err = " +
                            errCuenta);

                        if (errCuenta != 0)
                        {
                            throw new Exception(
                                "Error creando cuenta. Código: " +
                                errCuenta);
                        }

                        if (noInterno <= 0)
                        {
                            throw new Exception(
                                "El procedimiento mk_cuentas_insert " +
                                "no devolvió un NoInterno válido.");
                        }


                        // ====================================================
                        // PASO 2 - CONTACTO PRINCIPAL
                        // ====================================================

                        Log("========================================");
                        Log("PASO 2 - CREAR CONTACTO PRINCIPAL");
                        Log("========================================");

                        int coContacto =
                            CrearContactoPrincipal(
                                connection,
                                transaction,
                                noInterno,
                                fname,
                                lname,
                                out int errContacto);

                        Log(
                            "PASO 2 FINALIZADO. " +
                            "CoContacto = " +
                            coContacto +
                            " | Err = " +
                            errContacto);

                        if (errContacto != 0)
                        {
                            throw new Exception(
                                "Error creando contacto. Código: " +
                                errContacto);
                        }

                        if (coContacto <= 0)
                        {
                            throw new Exception(
                                "mk_contactos_insert " +
                                "no devolvió un CoContacto válido.");
                        }


                        // ====================================================
                        // PASO 3 - EMAIL
                        // ====================================================

                        Log("========================================");
                        Log("PASO 3 - CREAR EMAIL");
                        Log("========================================");

                        CrearMedioComunicacion(
                            connection,
                            transaction,
                            noInterno,
                            1,
                            email,
                            true,
                            out int errEmail);

                        Log(
                            "PASO 3 FINALIZADO. " +
                            "ErrEmail = " +
                            errEmail);

                        if (errEmail != 0)
                        {
                            throw new Exception(
                                "Error creando email. Código: " +
                                errEmail);
                        }


                        // ====================================================
                        // PASO 4 - TELEFONO
                        // ====================================================

                        Log("========================================");
                        Log("PASO 4 - CREAR TELEFONO");
                        Log("========================================");

                        CrearMedioComunicacion(
                            connection,
                            transaction,
                            noInterno,
                            0,
                            phone,
                            true,
                            out int errPhone);

                        Log(
                            "PASO 4 FINALIZADO. " +
                            "ErrPhone = " +
                            errPhone);

                        if (errPhone != 0)
                        {
                            throw new Exception(
                                "Error creando teléfono. Código: " +
                                errPhone);
                        }


                        // ====================================================
                        // PASO 5 - ACTIVIDAD
                        // ====================================================

                        Log("========================================");
                        Log("PASO 5 - CREAR ACTIVIDAD");
                        Log("========================================");

                        Log("CoParametro = 3");

                        Log(
                            "NoElemento = [" +
                            noElementoActividad +
                            "]");

                        Log(
                            "Contenido = [" +
                            actividad +
                            "]");

                        CrearParametroCuenta(
                            connection,
                            transaction,
                            noInterno,
                            3,
                            noElementoActividad,
                            actividad,
                            out int errActividad);

                        Log(
                            "PASO 5 FINALIZADO. " +
                            "ErrActividad = " +
                            errActividad);

                        if (errActividad != 0)
                        {
                            throw new Exception(
                                "Error creando actividad. Código: " +
                                errActividad);
                        }


                        // ====================================================
                        // PASO 6 - ORIGEN
                        // ====================================================

                        Log("========================================");
                        Log("PASO 6 - CREAR ORIGEN");
                        Log("========================================");

                        Log("CoParametro = 6");
                        Log("NoElemento  = [15]");
                        Log("Contenido   = <NULL>");

                        CrearParametroCuenta(
                            connection,
                            transaction,
                            noInterno,
                            6,
                            "15",
                            null,
                            out int errOrigen);

                        Log(
                            "PASO 6 FINALIZADO. " +
                            "ErrOrigen = " +
                            errOrigen);

                        if (errOrigen != 0)
                        {
                            throw new Exception(
                                "Error creando origen. Código: " +
                                errOrigen);
                        }

                        origen = "Contact Form PBP";

                        Log(
                            "Origen generado = [" +
                            origen +
                            "]"
                        );



                        // ============================================================
                        // PASO 7 - TRIAL USER NAME
                        // ============================================================

                        Log("========================================");
                        Log("PASO 7 - CREAR TRIAL USER NAME");
                        Log("========================================");

                        trialUserName = ConstruirTrialUserName(
                            fname,
                            lname);

                        Log(
                            "Trial User Name generado = [" +
                            trialUserName +
                            "]");

                        Log("CoParametro = 16");
                        Log("NoElemento  = [" + trialUserName + "]");
                        Log("Contenido   = <NULL>");

                        CrearParametroCuenta(
                            connection,
                            transaction,
                            noInterno,
                            16,
                            trialUserName,
                            null,
                            out int errTrialUserName);

                        Log(
                            "PASO 7 FINALIZADO. " +
                            "ErrTrialUserName = " +
                            errTrialUserName);

                        if (errTrialUserName != 0)
                        {
                            throw new Exception(
                                "Error creando Trial User Name. Código: " +
                                errTrialUserName);
                        }


                        // ============================================================
                        // PASO 8 - TRIAL PASSWORD
                        // ============================================================

                        Log("========================================");
                        Log("PASO 8 - CREAR TRIAL PASSWORD");
                        Log("========================================");

                        trialPassword = "password321+";

                        Log("CoParametro = 17");
                        Log("NoElemento  = [" + trialPassword + "]");
                        Log("Contenido   = <NULL>");

                        CrearParametroCuenta(
                            connection,
                            transaction,
                            noInterno,
                            17,
                            trialPassword,
                            null,
                            out int errTrialPassword);

                        Log(
                            "PASO 8 FINALIZADO. " +
                            "ErrTrialPassword = " +
                            errTrialPassword);

                        if (errTrialPassword != 0)
                        {
                            throw new Exception(
                                "Error creando Trial Password. Código: " +
                                errTrialPassword);
                        }


                        // ====================================================
                        // COMMIT
                        // ====================================================

                        Log("========================================");
                        Log("TODOS LOS PASOS SQL CORRECTOS");
                        Log("EJECUTANDO COMMIT");
                        Log("NoInterno = " + noInterno);
                        Log("========================================");

                        transaction.Commit();

                        Log("COMMIT OK");
                        Log("CONTACTO CREADO CORRECTAMENTE");

                        return true;
                    }
                    catch (SqlException ex)
                    {
                        Log("========================================");
                        Log("SQL EXCEPTION EN TRANSACCION");
                        Log("========================================");

                        Log("Number    : " + ex.Number);
                        Log("Message   : " + ex.Message);
                        Log("State     : " + ex.State);
                        Log("Class     : " + ex.Class);
                        Log("Line      : " + ex.LineNumber);
                        Log("Procedure : " + ex.Procedure);

                        Log("STACK TRACE:");
                        Log(ex.ToString());

                        try
                        {
                            transaction.Rollback();
                            Log("ROLLBACK OK");
                        }
                        catch (Exception rollbackEx)
                        {
                            Log("ERROR HACIENDO ROLLBACK:");
                            Log(rollbackEx.ToString());
                        }

                        noInterno = 0;
                        error = ex.ToString();

                        return false;
                    }
                    catch (Exception ex)
                    {
                        Log("========================================");
                        Log("EXCEPTION GENERAL EN TRANSACCION");
                        Log("========================================");

                        Log("Tipo:");
                        Log(ex.GetType().FullName);

                        Log("Mensaje:");
                        Log(ex.Message);

                        Log("STACK TRACE:");
                        Log(ex.StackTrace);

                        if (ex.InnerException != null)
                        {
                            Log("INNER EXCEPTION:");
                            Log(ex.InnerException.ToString());
                        }

                        try
                        {
                            transaction.Rollback();
                            Log("ROLLBACK OK");
                        }
                        catch (Exception rollbackEx)
                        {
                            Log("ERROR HACIENDO ROLLBACK:");
                            Log(rollbackEx.ToString());
                        }

                        noInterno = 0;
                        error = ex.ToString();

                        return false;
                    }
                }
            }
        }


        // ============================================================
        // CONSTRUIR TRIAL USER NAME
        // ============================================================
        //
        // Ejemplo:
        //
        // FNAME = Juan
        // LNAME = Perez
        //
        // Resultado base:
        // jPerez
        //
        // Máximo 10 caracteres:
        // jPerez
        //
        // Resultado final:
        // jPerez@baseglobal.com.ar
        //
        // ============================================================

        private static string ConstruirTrialUserName(
            string fname,
            string lname)
        {
            fname = (fname ?? "").Trim();
            lname = (lname ?? "").Trim();

            if (fname.Length == 0)
            {
                throw new Exception(
                    "No se puede construir Trial User Name: " +
                    "FNAME vacío.");
            }

            if (lname.Length == 0)
            {
                throw new Exception(
                    "No se puede construir Trial User Name: " +
                    "LNAME vacío.");
            }


            // Primera letra del nombre + apellido
            string usuario =
                fname.Substring(0, 1) +
                lname;


            // Máximo 10 caracteres
            if (usuario.Length > 10)
            {
                usuario =
                    usuario.Substring(0, 10);
            }


            // Normalizamos a minúsculas.
            usuario =
                usuario.ToLowerInvariant();


            // Dominio fijo
            return usuario +
                   "@baseglobal.com.ar";
        }


        // ============================================================
        // CREAR CUENTA
        // ============================================================

        private int CrearCuenta(
            SqlConnection connection,
            SqlTransaction transaction,
            string razonSocial,
            out int err)
        {
            err = 0;

            Log("----------------------------------------");
            Log("SP: mk_cuentas_insert");
            Log("RazonSocial = [" + razonSocial + "]");


            using (SqlCommand command =
                   new SqlCommand(
                       "mk_cuentas_insert",
                       connection,
                       transaction))
            {
                command.CommandType =
                    CommandType.StoredProcedure;


                // @Nointerno INT OUTPUT
                SqlParameter pNoInterno =
                command.Parameters.Add(
                    "@Nointerno",
                    SqlDbType.Int);

                            pNoInterno.Direction =
                                ParameterDirection.InputOutput;

                            pNoInterno.Value = 0;


                command.Parameters.Add(
                    "@Cocuenta",
                    SqlDbType.NVarChar,
                    20).Value = "";


                command.Parameters.Add(
                    "@Razonsocial",
                    SqlDbType.NVarChar,
                    50).Value =
                        Truncate(razonSocial, 50);


                command.Parameters.Add(
                    "@Denominacion",
                    SqlDbType.NVarChar,
                    50).Value =
                        Truncate(razonSocial, 50);


                command.Parameters.Add(
                    "@Domicilio",
                    SqlDbType.NVarChar,
                    50).Value = "";


                command.Parameters.Add(
                    "@Localidad",
                    SqlDbType.NVarChar,
                    50).Value = "";


                command.Parameters.Add(
                    "@Cocalle",
                    SqlDbType.Int).Value = 0;


                command.Parameters.Add(
                    "@Nopuerta",
                    SqlDbType.NVarChar,
                    20).Value = "";


                command.Parameters.Add(
                    "@Cocalleentre1",
                    SqlDbType.Int).Value = 0;


                command.Parameters.Add(
                    "@Cocalleentre2",
                    SqlDbType.Int).Value = 0;


                command.Parameters.Add(
                    "@Colocalidad",
                    SqlDbType.Int).Value = 0;


                command.Parameters.Add(
                    "@Copostal",
                    SqlDbType.NVarChar,
                    10).Value = "";


                command.Parameters.Add(
                    "@CoPais",
                    SqlDbType.SmallInt).Value = 0;


                command.Parameters.Add(
                    "@Coprovincia",
                    SqlDbType.SmallInt).Value = 0;


                command.Parameters.Add(
                    "@Coestado",
                    SqlDbType.SmallInt).Value = 0;


                command.Parameters.Add(
                    "@Nointernoref",
                    SqlDbType.Int).Value = 0;


                command.Parameters.Add(
                    "@Cocontactoref",
                    SqlDbType.SmallInt).Value = 0;


                command.Parameters.Add(
                    "@Observaciones",
                    SqlDbType.VarChar,
                    -1).Value = "";


                command.Parameters.Add(
                    "@Fechaalta",
                    SqlDbType.DateTime).Value =
                        DateTime.Now;


                command.Parameters.Add(
                    "@Cousuarioalta",
                    SqlDbType.NVarChar,
                    20).Value =
                        "CONTACTFORM";


                command.Parameters.Add(
                    "@Cogrupoalta",
                    SqlDbType.NVarChar,
                    20).Value = "";


                command.Parameters.Add(
                    "@Fechaultimamodificacion",
                    SqlDbType.DateTime).Value =
                        DateTime.Now;


                command.Parameters.Add(
                    "@Cousuariomodificacion",
                    SqlDbType.NVarChar,
                    20).Value =
                        "CONTACTFORM";


                command.Parameters.Add(
                    "@Cogrupomodificacion",
                    SqlDbType.NVarChar,
                    20).Value = "";


                command.Parameters.Add(
                    "@Sitioweb",
                    SqlDbType.NVarChar,
                    50).Value = "";


                command.Parameters.Add(
                    "@Imporigen",
                    SqlDbType.SmallInt).Value = 0;


                command.Parameters.Add(
                    "@Impcuenta",
                    SqlDbType.Int).Value = 0;


                command.Parameters.Add(
                    "@Cuentaweb",
                    SqlDbType.Bit).Value = false;


                command.Parameters.Add(
                    "@Noserie",
                    SqlDbType.NVarChar,
                    30).Value = "";


                command.Parameters.Add(
                    "@Cointegrador",
                    SqlDbType.Int).Value = 0;


                command.Parameters.Add(
                    "@Updateuser",
                    SqlDbType.VarChar,
                    80).Value =
                        "CONTACTFORM";


                command.Parameters.Add(
                    "@cotipodocumento",
                    SqlDbType.Int).Value = 0;


                command.Parameters.Add(
                    "@nodocumento",
                    SqlDbType.VarChar,
                    100).Value = "0";


                // @Err OUTPUT
                SqlParameter pErr =
                    command.Parameters.Add(
                        "@Err",
                        SqlDbType.Int);

                pErr.Direction =
                    ParameterDirection.Output;


                // ========================================================
                // LOG PARAMETROS
                // ========================================================

                Log("PARAMETROS:");

                LogParametros(command);


                // ========================================================
                // EJECUTAR SP
                // ========================================================

                Log("EJECUTANDO mk_cuentas_insert...");

                try
                {
                    command.ExecuteNonQuery();

                    Log(
                        "mk_cuentas_insert EJECUTADO CORRECTAMENTE.");
                }
                catch (SqlException ex)
                {
                    Log("ERROR SQL EN mk_cuentas_insert");

                    Log("Number    : " + ex.Number);
                    Log("Message   : " + ex.Message);
                    Log("State     : " + ex.State);
                    Log("Class     : " + ex.Class);
                    Log("Line      : " + ex.LineNumber);
                    Log("Procedure : " + ex.Procedure);

                    Log("STACK TRACE:");
                    Log(ex.ToString());

                    throw;
                }
                catch (Exception ex)
                {
                    Log("ERROR .NET EN mk_cuentas_insert");
                    Log(ex.ToString());

                    throw;
                }


                // ========================================================
                // OUTPUTS
                // ========================================================

                Log("OUTPUTS mk_cuentas_insert:");

                string valorNoInterno =
                    pNoInterno.Value == DBNull.Value
                        ? "<DBNull>"
                        : pNoInterno.Value.ToString();

                string valorErr =
                    pErr.Value == DBNull.Value
                        ? "<DBNull>"
                        : pErr.Value.ToString();

                Log("@Nointerno = [" + valorNoInterno + "]");
                Log("@Err       = [" + valorErr + "]");


                if (pErr.Value != DBNull.Value)
                {
                    err =
                        Convert.ToInt32(
                            pErr.Value);
                }
                else
                {
                    err = 0;

                    Log(
                        "ADVERTENCIA: @Err devolvió DBNull.");
                }


                if (pNoInterno.Value == DBNull.Value)
                {
                    Log(
                        "ERROR: @Nointerno devolvió DBNull.");

                    throw new Exception(
                        "mk_cuentas_insert no devolvió @Nointerno.");
                }


                int resultado =
                    Convert.ToInt32(
                        pNoInterno.Value);


                Log(
                    "NoInterno convertido = " +
                    resultado);

                return resultado;
            }
        }


        // ============================================================
        // CREAR CONTACTO PRINCIPAL
        // ============================================================

        private int CrearContactoPrincipal(
            SqlConnection connection,
            SqlTransaction transaction,
            int noInterno,
            string nombres,
            string apellido,
            out int err)
        {
            err = 0;

            Log("----------------------------------------");
            Log("SP: mk_contactos_insert");
            Log("NoInterno = " + noInterno);
            Log("Nombres   = [" + nombres + "]");
            Log("Apellido  = [" + apellido + "]");


            using (SqlCommand command =
                   new SqlCommand(
                       "mk_contactos_insert",
                       connection,
                       transaction))
            {
                command.CommandType =
                    CommandType.StoredProcedure;


                command.Parameters.Add(
                    "@nointerno",
                    SqlDbType.Int).Value =
                        noInterno;


                SqlParameter pCoContacto =
                    command.Parameters.Add(
                        "@cocontacto",
                        SqlDbType.SmallInt);

                pCoContacto.Direction =
                    ParameterDirection.Output;


                command.Parameters.Add(
                    "@apellido",
                    SqlDbType.NVarChar,
                    50).Value =
                        Truncate(apellido, 50);


                command.Parameters.Add(
                    "@nombres",
                    SqlDbType.NVarChar,
                    50).Value =
                        Truncate(nombres, 50);


                command.Parameters.Add(
                    "@cotipodocumento",
                    SqlDbType.Int).Value = 0;

                command.Parameters.Add(
                    "@nodocumento",
                    SqlDbType.VarChar,
                    100).Value = "0";



                command.Parameters.Add(
                    "@coapellido1",
                    SqlDbType.SmallInt).Value = 0;


                command.Parameters.Add(
                    "@coapellido2",
                    SqlDbType.Int).Value = 0;


                command.Parameters.Add(
                    "@conombre1",
                    SqlDbType.SmallInt).Value = 0;


                command.Parameters.Add(
                    "@conombre2",
                    SqlDbType.SmallInt).Value = 0;


                command.Parameters.Add(
                    "@conombre3",
                    SqlDbType.SmallInt).Value = 0;


                command.Parameters.Add(
                    "@contactoprincipal",
                    SqlDbType.Bit).Value = true;


                command.Parameters.Add(
                    "@cocargo",
                    SqlDbType.SmallInt).Value = 0;


                command.Parameters.Add(
                    "@codepartamento",
                    SqlDbType.SmallInt).Value = 0;


                command.Parameters.Add(
                    "@observaciones",
                    SqlDbType.VarChar,
                    -1).Value = "";


                command.Parameters.Add(
                    "@updateuser",
                    SqlDbType.VarChar,
                    80).Value =
                        "CONTACTFORM";


                SqlParameter pErr =
                    command.Parameters.Add(
                        "@err",
                        SqlDbType.Int);

                pErr.Direction =
                    ParameterDirection.Output;


                Log("PARAMETROS:");

                LogParametros(command);


                Log("EJECUTANDO mk_contactos_insert...");

                try
                {
                    command.ExecuteNonQuery();

                    Log(
                        "mk_contactos_insert EJECUTADO CORRECTAMENTE.");
                }
                catch (SqlException ex)
                {
                    Log("ERROR SQL EN mk_contactos_insert");

                    Log("Number    : " + ex.Number);
                    Log("Message   : " + ex.Message);
                    Log("State     : " + ex.State);
                    Log("Class     : " + ex.Class);
                    Log("Line      : " + ex.LineNumber);
                    Log("Procedure : " + ex.Procedure);

                    Log(ex.ToString());

                    throw;
                }
                catch (Exception ex)
                {
                    Log("ERROR .NET EN mk_contactos_insert");
                    Log(ex.ToString());

                    throw;
                }


                string valorContacto =
                    pCoContacto.Value == DBNull.Value
                        ? "<DBNull>"
                        : pCoContacto.Value.ToString();

                string valorErr =
                    pErr.Value == DBNull.Value
                        ? "<DBNull>"
                        : pErr.Value.ToString();

                Log("OUTPUTS:");
                Log("@CoContacto = [" + valorContacto + "]");
                Log("@Err        = [" + valorErr + "]");


                if (pErr.Value != DBNull.Value)
                {
                    err =
                        Convert.ToInt32(
                            pErr.Value);
                }
                else
                {
                    err = 0;

                    Log(
                        "ADVERTENCIA: @Err devolvió DBNull.");
                }


                if (pCoContacto.Value == DBNull.Value)
                {
                    Log(
                        "ERROR: @CoContacto devolvió DBNull.");

                    throw new Exception(
                        "mk_contactos_insert no devolvió @CoContacto.");
                }


                int resultado =
                    Convert.ToInt32(
                        pCoContacto.Value);


                Log(
                    "CoContacto convertido = " +
                    resultado);

                return resultado;
            }
        }


        // ============================================================
        // CREAR EMAIL / TELEFONO
        // ============================================================

        private void CrearMedioComunicacion(
            SqlConnection connection,
            SqlTransaction transaction,
            int noInterno,
            short coMedioComunicacion,
            string descripcion,
            bool principal,
            out int err)
        {
            err = 0;

            Log("----------------------------------------");
            Log("SP: mk_canalcomunicacioncuentas_insert");
            Log("NoInterno = " + noInterno);
            Log("CoMedioComunicacion = " +
                coMedioComunicacion);
            Log("Descripcion = [" +
                descripcion +
                "]");
            Log("Principal = " +
                principal);


            using (SqlCommand command =
                   new SqlCommand(
                       "mk_canalcomunicacioncuentas_insert",
                       connection,
                       transaction))
            {
                command.CommandType =
                    CommandType.StoredProcedure;


                command.Parameters.Add(
                    "@nointerno",
                    SqlDbType.Int).Value =
                        noInterno;


                command.Parameters.Add(
                    "@comediocomunicacion",
                    SqlDbType.SmallInt).Value =
                        coMedioComunicacion;


                command.Parameters.Add(
                    "@descripcion",
                    SqlDbType.NVarChar,
                    100).Value =
                        Truncate(descripcion, 100);


                command.Parameters.Add(
                    "@principal",
                    SqlDbType.Bit).Value =
                        principal;


                command.Parameters.Add(
                    "@updateuser",
                    SqlDbType.VarChar,
                    80).Value =
                        "CONTACTFORM";


                SqlParameter pErr =
                    command.Parameters.Add(
                        "@err",
                        SqlDbType.Int);

                pErr.Direction =
                    ParameterDirection.Output;


                Log("PARAMETROS:");

                LogParametros(command);


                Log(
                    "EJECUTANDO " +
                    "mk_canalcomunicacioncuentas_insert...");


                try
                {
                    command.ExecuteNonQuery();

                    Log(
                        "mk_canalcomunicacioncuentas_insert " +
                        "EJECUTADO CORRECTAMENTE.");
                }
                catch (SqlException ex)
                {
                    Log(
                        "ERROR SQL EN " +
                        "mk_canalcomunicacioncuentas_insert");

                    Log("Number    : " + ex.Number);
                    Log("Message   : " + ex.Message);
                    Log("State     : " + ex.State);
                    Log("Class     : " + ex.Class);
                    Log("Line      : " + ex.LineNumber);
                    Log("Procedure : " + ex.Procedure);

                    Log(ex.ToString());

                    throw;
                }
                catch (Exception ex)
                {
                    Log(
                        "ERROR .NET EN " +
                        "mk_canalcomunicacioncuentas_insert");

                    Log(ex.ToString());

                    throw;
                }


                string valorErr =
                    pErr.Value == DBNull.Value
                        ? "<DBNull>"
                        : pErr.Value.ToString();

                Log(
                    "@Err = [" +
                    valorErr +
                    "]");


                if (pErr.Value != DBNull.Value)
                {
                    err =
                        Convert.ToInt32(
                            pErr.Value);
                }
                else
                {
                    err = 0;

                    Log(
                        "ADVERTENCIA: @Err devolvió DBNull.");
                }
            }
        }


        // ============================================================
        // CREAR PARAMETRO DE CUENTA
        // ============================================================

        private void CrearParametroCuenta(
            SqlConnection connection,
            SqlTransaction transaction,
            int noInterno,
            short coParametro,
            string noElemento,
            string contenido,
            out int err)
        {
            err = 0;

            Log("----------------------------------------");
            Log("SP: mk_parametros_cuentas_insert");
            Log("NoInterno = " + noInterno);
            Log("CoParametro = " + coParametro);
            Log("NoElemento = [" + noElemento + "]");

            if (contenido == null)
            {
                Log("Contenido = <NULL>");
            }
            else if (coParametro == 17)
            {
                // Nunca mostramos la contraseña en texto plano.
                Log("Contenido = [********]");
            }
            else
            {
                Log("Contenido = [" + contenido + "]");
            }


            using (SqlCommand command =
                   new SqlCommand(
                       "mk_parametros_cuentas_insert",
                       connection,
                       transaction))
            {
                command.CommandType =
                    CommandType.StoredProcedure;


                command.Parameters.Add(
                    "@nointerno",
                    SqlDbType.Int).Value =
                        noInterno;


                command.Parameters.Add(
                    "@coparametro",
                    SqlDbType.SmallInt).Value =
                        coParametro;


                // Para parámetros cuyo CoTipoDato es distinto de 0 y 99,
                // el Stored Procedure utiliza @noelemento como contenido.
                // Esto aplica, entre otros, a los parámetros 16 y 17.
                command.Parameters.Add(
                    "@noelemento",
                    SqlDbType.NVarChar,
                    -1).Value =
                        string.IsNullOrEmpty(noElemento)
                            ? (object)DBNull.Value
                            : noElemento;


                command.Parameters.Add(
                    "@contenido",
                    SqlDbType.NVarChar,
                    -1).Value =
                        string.IsNullOrEmpty(contenido)
                            ? (object)DBNull.Value
                            : contenido;


                command.Parameters.Add(
                    "@updateuser",
                    SqlDbType.VarChar,
                    80).Value =
                        "CONTACTFORM";


                SqlParameter pErr =
                    command.Parameters.Add(
                        "@err",
                        SqlDbType.Int);

                pErr.Direction =
                    ParameterDirection.Output;


                Log("PARAMETROS:");

                LogParametros(command);


                Log(
                    "EJECUTANDO " +
                    "mk_parametros_cuentas_insert...");


                try
                {
                    command.ExecuteNonQuery();

                    Log(
                        "mk_parametros_cuentas_insert " +
                        "EJECUTADO CORRECTAMENTE.");
                }
                catch (SqlException ex)
                {
                    Log(
                        "ERROR SQL EN " +
                        "mk_parametros_cuentas_insert");

                    Log("Number    : " + ex.Number);
                    Log("Message   : " + ex.Message);
                    Log("State     : " + ex.State);
                    Log("Class     : " + ex.Class);
                    Log("Line      : " + ex.LineNumber);
                    Log("Procedure : " + ex.Procedure);

                    Log(ex.ToString());

                    throw;
                }
                catch (Exception ex)
                {
                    Log(
                        "ERROR .NET EN " +
                        "mk_parametros_cuentas_insert");

                    Log(ex.ToString());

                    throw;
                }


                string valorErr =
                    pErr.Value == DBNull.Value
                        ? "<DBNull>"
                        : pErr.Value.ToString();

                Log(
                    "@Err = [" +
                    valorErr +
                    "]");


                if (pErr.Value != DBNull.Value)
                {
                    err =
                        Convert.ToInt32(
                            pErr.Value);
                }
                else
                {
                    err = 0;

                    Log(
                        "ADVERTENCIA: @Err devolvió DBNull.");
                }
            }
        }


        // ============================================================
        // UTILIDAD
        // ============================================================

        private static string Truncate(
            string value,
            int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            if (value.Length <= maxLength)
                return value;

            return value.Substring(
                0,
                maxLength);
        }
    }
}
