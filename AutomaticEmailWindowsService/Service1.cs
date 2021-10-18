using MySql.Data.MySqlClient;
using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Net.Mail;
using System.ServiceProcess;
using System.Threading;

namespace AutomaticEmailWindowsService
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            this.WriteToFile("AutomatedEmailService started {0}");
            this.ScheduleService();
        }

        protected override void OnStop()
        {
            this.WriteToFile("AutomatedEmailService stopped {0}");
            this.Schedular.Dispose();
        }

        private Timer Schedular;

        public void ScheduleService()
        {
            try
            {
                Schedular = new Timer(new TimerCallback(SchedularCallback));
                string mode = ConfigurationManager.AppSettings["Mode"].ToUpper();
                this.WriteToFile("AutomatedEmailService Mode: " + mode + " {0}");

                //Set the Default Time.
                DateTime scheduledTime = DateTime.MinValue;

                if (mode == "DAILY")
                {
                    //Get the Scheduled Time from AppSettings.
                    scheduledTime = DateTime.Parse(System.Configuration.ConfigurationManager.AppSettings["ScheduledTime"]);
                    if (DateTime.Now > scheduledTime)
                    {
                        //If Scheduled Time is passed set Schedule for the next day.
                        scheduledTime = scheduledTime.AddDays(1);
                    }
                }

                if (mode.ToUpper() == "INTERVAL")
                {
                    //Get the Interval in Minutes from AppSettings.
                    int intervalMinutes = Convert.ToInt32(ConfigurationManager.AppSettings["IntervalMinutes"]);

                    //Set the Scheduled Time by adding the Interval to Current Time.
                    scheduledTime = DateTime.Now.AddMinutes(intervalMinutes);
                    if (DateTime.Now > scheduledTime)
                    {
                        //If Scheduled Time is passed set Schedule for the next Interval.
                        scheduledTime = scheduledTime.AddMinutes(intervalMinutes);
                    }
                }

                TimeSpan timeSpan = scheduledTime.Subtract(DateTime.Now);
                string schedule = string.Format("{0} day(s) {1} hour(s) {2} minute(s) {3} seconds(s)", timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);

                this.WriteToFile("AutomatedEmailService scheduled to run after: " + schedule + " {0}");

                //Get the difference in Minutes between the Scheduled and Current Time.
                int dueTime = Convert.ToInt32(timeSpan.TotalMilliseconds);

                //Change the Timer's Due Time.
                Schedular.Change(dueTime, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                WriteToFile("AutomatedEmailService Error on: {0} " + ex.Message + ex.StackTrace);

                //Stop the Windows Service.
                using (System.ServiceProcess.ServiceController serviceController = new System.ServiceProcess.ServiceController("AutomatedEmailService"))
                {
                    serviceController.Stop();
                }
            }
        }

        private void SchedularCallback(object e)
        {
            try
            {
                DataTable dt = new DataTable();
                //string query = "SELECT Name, Email FROM students WHERE DATEPART(DAY, BirthDate) = @Day AND DATEPART(MONTH, BirthDate) = @Month";
                string query = "SELECT Name, Email FROM students";
                string constr = ConfigurationManager.ConnectionStrings["constr"].ConnectionString;

                using (MySqlConnection con = new MySqlConnection(constr))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query))
                    {
                        cmd.Connection = con;
                        //cmd.Parameters.AddWithValue("@Day", DateTime.Today.Day);
                        //cmd.Parameters.AddWithValue("@Month", DateTime.Today.Month);
                        using (MySqlDataAdapter sda = new MySqlDataAdapter(cmd))
                        {
                            sda.Fill(dt);
                        }
                    }
                }

                foreach (DataRow row in dt.Rows)
                {
                    string name = row["Name"].ToString();
                    string email = row["Email"].ToString();
                    WriteToFile("Trying to send email to: " + name + " " + email);

                    using (MailMessage mm = new MailMessage("shohan.mirja@gmail.com", email))
                    {
                        mm.Subject = "Birthday Greetings";
                        mm.Body = string.Format("<b>Happy Birthday </b>{0}<br /><br />Many Many happy returns of the day.", name);

                        mm.IsBodyHtml = true;
                        SmtpClient smtp = new SmtpClient();
                        smtp.Host = "smtp.gmail.com";
                        smtp.EnableSsl = true;
                        System.Net.NetworkCredential credentials = new System.Net.NetworkCredential();
                        credentials.UserName = "shohan.mirja@gmail.com";
                        credentials.Password = "google9874105";
                        smtp.UseDefaultCredentials = true;
                        smtp.Credentials = credentials;
                        smtp.Port = 587;
                        smtp.Send(mm);
                        WriteToFile("Email sent successfully to: " + name + " " + email);
                    }
                }

                this.ScheduleService();
            }
            catch (Exception ex)
            {

                WriteToFile("AutomatedEmailService Error on: {0} " + ex.Message + ex.StackTrace);

                //Stop the Windows Service.
                using (System.ServiceProcess.ServiceController serviceController = new System.ServiceProcess.ServiceController("AutomatedEmailService"))
                {
                    serviceController.Stop();
                }
            }
        }

        private void WriteToFile(string text)
        {
            string path = "C:\\ServiceLog.txt";
            using (StreamWriter writer = new StreamWriter(path, true))
            {
                writer.WriteLine(string.Format(text, DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt")));
                writer.Close();
            }
        }
    }
}
