using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Net.Mail;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SendEmails
{
    class Program
    {
        static void Main(string[] args)
        {           
            DataTable dtEmailInfo = GetEmail();
            
            if (dtEmailInfo != null && dtEmailInfo.Rows.Count > 0)
            {               
                int emailKey = 0;
                string emailReceiver = "";
                string emailCC = "";
                string emailSubject = "";
                string emailBody = "";
                string emailAttachment = "";
                string oMSever = ConfigurationManager.AppSettings["OutgoingMailServer"];
                string emailFrom = ConfigurationManager.AppSettings["AuthenticatedMailSender"];
                string encryptPW = ConfigurationManager.AppSettings["EncryptPassword"];
                string emailPW = ConfigurationManager.AppSettings["Password"];
                string emailDisplayName = ConfigurationManager.AppSettings["DisplayName"];
                int portNo = Int16.Parse(ConfigurationManager.AppSettings["PortNo"]);

                //emailPW = TAUtil.Encoder.Encode("Vugu9584"); /* VnVndTk1ODQ= */
                emailPW = encryptPW.ToLower() == "true" ? TAUtil.Decoder.Decode(emailPW): emailPW;

                foreach (DataRow dr in dtEmailInfo.Rows)
                {
                    if ((NEStr(dr["EmailFrom"], "")) == "MailProfile_AR")
                    {
                        emailFrom = "ar@bhglobal.com.sg";
                        emailPW = "pjnrprjkwqyhlkcz";
                        emailDisplayName = "BHG - AR";
                    } 
                    else if ((NEStr(dr["EmailFrom"], "")) == "ADPLFinance")
                    {
                        emailFrom = "finance@athenadynamics.com";
                        emailPW = "wzpqlxlpjsvdfrrq";
                        emailDisplayName = "ADPL - Finance";
                    }
                    else if ((NEStr(dr["EmailFrom"], "")) == "BHGPayment" || (NEStr(dr["EmailFrom"], "")) == "MailProfile2")
                    {
                        emailFrom = "payment@bhglobal.com.sg";
                        emailPW = "wzpqlxlpjsvdfrrq";
                        emailDisplayName = "BHG - Payment";
                    }
                    emailKey = NEInt(dr["EmailKey"], "");
                    emailReceiver = NEStr(dr["EmailTo"], "");
                    emailCC = NEStr(dr["EmailCC"], "");
                    emailSubject = NEStr(dr["EmailSubject"], "");
                    emailBody = NEStr(dr["EmailBody"], "");
                    emailAttachment = NEStr(dr["EmailAttachPath"], "");
                    SendEmail(emailFrom, emailPW, emailDisplayName, oMSever, portNo, emailReceiver, emailCC, emailSubject, emailBody, emailKey, emailAttachment);
                }
            }

            dtEmailInfo.Dispose();
        }
        static DataTable GetEmail()
        {
            using (SqlConnection cn = new SqlConnection(EmailSendingDBConnection))
            {
                DataTable dt = null;
                cn.Open();
                try
                {
                    List<SqlParameter> parmList = new List<SqlParameter>();
                    parmList.Add(new SqlParameter("@Option", 0));
                    dt = ExecuteProc(cn, "SysEmailQueue_Get", parmList);
                }
                catch (Exception ex)
                {
                    //
                }
                return dt;
            }
        }
        static void DeleteEmail(int emailKey, bool hasError  = false)
        {
            using (SqlConnection cn = new SqlConnection(EmailSendingDBConnection))
            {              
                cn.Open();
                try
                {
                    List<SqlParameter> parmList = new List<SqlParameter>();
                    parmList.Add(new SqlParameter("@emailKey", emailKey));
                    parmList.Add(new SqlParameter("@hasError", hasError));
                    ExecuteNonQueryProc("SysEmailQueue_UpdateSentStatus", parmList);
                }
                catch (Exception ex)
                {
                    //
                }
            }
        }
        static void ExecuteNonQueryProc(string procName, List<SqlParameter> parmList)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(EmailSendingDBConnection))
                {
                    connection.Open();
                    using (SqlCommand command = connection.CreateCommand())
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = procName;
                        if (parmList != null)
                        {
                            foreach (SqlParameter parameter in parmList)
                            {
                                if (parameter.Value == null)
                                {
                                    command.Parameters.Add(parameter);
                                }
                                else
                                {
                                    command.Parameters.Add(parameter);
                                }
                            }
                        }
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }
        static DataTable ExecuteProc(SqlConnection sqlCon, string procName, List<SqlParameter> parmList)
        {
            DataTable table2;
            try
            {
                DataSet dataSet = new DataSet();
                using (SqlCommand command = sqlCon.CreateCommand())
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = procName;
                    command.CommandTimeout = 100;
                    if (parmList != null)
                    {
                        foreach (SqlParameter parameter in parmList)
                        {
                            if (parameter.Value == null)
                            {
                                command.Parameters.Add(parameter);
                            }
                            else
                            {
                                command.Parameters.Add(parameter);
                            }
                        }
                    }
                    new SqlDataAdapter(command).Fill(dataSet);
                    DataTable table = null;
                    if (dataSet.Tables.Count > 0)
                    {
                        table = dataSet.Tables[0];
                    }
                    table2 = table;
                }
            }
            catch (Exception exception)
            {
                throw exception;
            }
            return table2;
        }
        static int NEInt(object value, object replaceValue)
        {
            int num;
            try
            {
                if (((value == null) || string.IsNullOrEmpty(value.ToString())) || (value == DBNull.Value))
                {
                    return Convert.ToInt32(replaceValue);
                }
                if (value.ToString().Contains("."))
                {
                    return decimal.ToInt32(Convert.ToDecimal(value));
                }
                num = Convert.ToInt32(value);
            }
            catch (Exception exception)
            {
                throw exception;
            }
            return num;
        }
        static string NEStr(object value, object replaceValue)
        {
            string str;
            try
            {
                if (((value == null) || string.IsNullOrEmpty(value.ToString())) || (value == DBNull.Value))
                {
                    return replaceValue.ToString();
                }
                str = value.ToString();
            }
            catch (Exception exception)
            {
                throw exception;
            }
            return str;
        }
        static bool SendEmail( string emailFrom, string emailPW, string emailDisplayName, string oMSever, int portNo,
                               string emailReceiver, string emailCC, string emailSubject, string emailBody, int emailKey, string emailAttachment)
        {
            bool HasError = false;
            try
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)(48 | 192 | 768 | 3072);
                string[] emailCc = emailCC.Split(';');
                string[] emailTo = emailReceiver.Trim().Split(';');
                string[] fileAttachment = emailAttachment.Trim().Split(';');

                Attachment Attachment = null;
                MailMessage email = new MailMessage();

                /* get receiver emails */
                for (int i = 0; i < emailTo.Length; i++)
                {
                    if(emailTo[i] != "") email.To.Add(emailTo[i]);
                }

                /* get CC emails */
                for (int i = 0; i < emailCc.Length; i++)
                {
                    if (emailCc[i] != "") email.CC.Add(emailCc[i]);
                }

                /* get file attachments */
                for (int i = 0; i < fileAttachment.Length; i++)
                {
                    if (!string.IsNullOrEmpty(fileAttachment[i]))
                    {
                        Attachment = new Attachment(fileAttachment[i]);
                        if (Attachment != null)
                            email.Attachments.Add(Attachment);
                    }
                }                

                email.From = new MailAddress(emailFrom, emailDisplayName);
                email.Subject = emailSubject;
                email.Body = emailBody;
                email.IsBodyHtml = true;

                //Sending Mail
                SmtpClient client = new SmtpClient(oMSever, portNo);
                client.EnableSsl = true;
                client.TargetName = "STARTTLS/smtp.office365.com";
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(emailFrom, emailPW);
                client.Send(email);
                email.Dispose();
                DeleteEmail(emailKey);
            }
            catch (Exception ex)
            {
                HasError = true;
                DeleteEmail(emailKey,HasError);
            }
            return HasError;
        }
        static string GetDBConnection()
        {
            string encryptDBConnection = ConfigurationManager.AppSettings["EncryptConnection"];
            string dbConnection = ConfigurationManager.ConnectionStrings["EmailSendingDBConnection"].ConnectionString;
            dbConnection = encryptDBConnection.ToLower() == "true" ? TAUtil.Decoder.Decode(dbConnection) : dbConnection;
            //dbConnection = TAUtil.Encoder.Encode("Database=BossBHM;Server=BHGSGPDB02;user id=DevAdmin;password=BH609189;Connect Timeout=0");
            return dbConnection;
        }
        static string EmailSendingDBConnection
        {
            get
            {
                return GetDBConnection();
            }            
        }
    }
}
