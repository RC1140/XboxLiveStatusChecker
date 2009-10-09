using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace TestApp
{
    public partial class LiveStatuses : Form
    {
        private DataTable liveUsers;
        List<string> usersToCheck = new List<string>();
        private BackgroundWorker[] workingGamers;

        public LiveStatuses()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            #region Setup Datatable
            liveUsers = new DataTable("liveUsers");
            liveUsers.Columns.Add("gamercard");
            liveUsers.Columns.Add("online");
            liveUsers.Columns.Add("last_seen_online");

            liveUsers.PrimaryKey = new DataColumn[] { liveUsers.Columns["gamercard"] };
            #endregion
        }

        private void GetUsersStatus(string Gamercard)
        {
            try
            {
                DataRow userInfo = liveUsers.Rows.Find(Gamercard);
                bool newData = (userInfo == null);
                if (newData)
                {
                    userInfo = liveUsers.NewRow();
                }

                HttpWebRequest sd =
                    (HttpWebRequest)HttpWebRequest.Create("http://xboxapi.duncanmackenzie.net/gamertag.ashx?GamerTag=" + Gamercard);

                StreamReader sddd = new StreamReader(sd.GetResponse().GetResponseStream());

                string xml = sddd.ReadToEnd();
                int startpoint = xml.IndexOf("Online");
                string status = xml.Substring(startpoint + 7, xml.IndexOf("Online", startpoint + 1) - startpoint);
                int startpoint2 = xml.IndexOf("LastSeen");
                string lastSeenOnline = xml.Substring(startpoint2 + 9, xml.IndexOf("LastSeen", startpoint2 + 1) - startpoint2);

                userInfo["gamercard"] = Gamercard;
                userInfo["online"] = status.Substring(0, status.Length - 9);
                userInfo["last_seen_online"] = lastSeenOnline.Substring(0, lastSeenOnline.Length - 11);

                if (newData)
                {
                    liveUsers.Rows.Add(userInfo);
                }
            }
            catch 
            {

            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            int totalNumberOfUsers = usersToCheck.Count;
            int groupingIndex = 0;
            decimal numberOfWorkersRequired = decimal.Round(decimal.Divide(totalNumberOfUsers, 20), 0,MidpointRounding.AwayFromZero);
            if (numberOfWorkersRequired == 0)
            {
                numberOfWorkersRequired = 1;
            }
            lblStatus.Text = "Updating";

            workingGamers = new BackgroundWorker[Convert.ToInt32(numberOfWorkersRequired)];
            for (int i = 0; i < workingGamers.Length; i++)
            {
                workingGamers[i] = new BackgroundWorker();
                workingGamers[i].DoWork += new DoWorkEventHandler(backgroundWorker1_DoWork);
                workingGamers[i].RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);
            }
            
            for (int i = 0; i < workingGamers.Length; i++)
            {
                totalNumberOfUsers -= 20;
                if (totalNumberOfUsers >= 20)
                {
                    workingGamers[i].RunWorkerAsync(usersToCheck.GetRange(groupingIndex, 20));
                }
                else
                {
                    workingGamers[i].RunWorkerAsync(usersToCheck.GetRange(groupingIndex, (20 + totalNumberOfUsers)));
                    break;
                }
                groupingIndex += 20;
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            UpdateUsersStatus(e);
        }

        private void UpdateUsersStatus(DoWorkEventArgs e)
        {
            List<string> pUsersToCheck = (List<string>)e.Argument;
            foreach (var gamertag in pUsersToCheck)
            {
                GetUsersStatus(gamertag);
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            lblNumUsersOnline.Text = liveUsers.Select("online = 'true'").Length.ToString();
            dgUsers.DataSource = liveUsers;
            dgUsers.Refresh();
            lblStatus.Text = "Update Complete";
        }

        private void btnLoadUsers_Click(object sender, EventArgs e)
        {
            if (openFileDialogUsers.ShowDialog() == DialogResult.OK)
            {
                usersToCheck.Clear();

                StreamReader sr = new StreamReader(openFileDialogUsers.FileName);

                while (!sr.EndOfStream)
                {
                    usersToCheck.Add(sr.ReadLine());
                }
                lblStatus.Text = "Users Loaded Successfully";
            }
        }
    }
}
