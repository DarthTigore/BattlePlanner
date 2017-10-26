using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

// Google APIs
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace BattlePlanner
{
    public class Spreadsheet
    {
        public static Spreadsheet Singleton = null;

        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/sheets.googleapis.com-dotnet-quickstart.json
        static string[] Scopes = { SheetsService.Scope.Spreadsheets };
        static string ApplicationName = "BattlePlanner";

        UserCredential Credential = null;
        SheetsService Service = null;

        public Spreadsheet()
        {
            // create Google Sheets credential
            using (var stream =
                new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.Personal);

                Credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            // create Google Sheets API service.
            Service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = Credential,
                ApplicationName = ApplicationName,
            });

            Singleton = this;
        }

        /// <summary>
        /// Get the full link with the ID
        /// </summary>
        /// <returns></returns>
        public static string GetLink()
        {
            var result = string.Format("https://docs.google.com/spreadsheets/d/{0}/edit", Settings.SpreadsheetID);
            return (Settings.SpreadsheetID.Length > 0) ? result : string.Empty;
        }

        /// <summary>
        /// Make sure the user provided an ID
        /// </summary>
        /// <returns></returns>
        private bool IsInitialized()
        {
            return (Settings.SpreadsheetID != null && Settings.SpreadsheetID.Length > 0);
        }

        /// <summary>
        /// Get the phase from the Platoons tab
        /// </summary>
        /// <returns></returns>
        public int GetPhase()
        {
            if (!IsInitialized())
            {
                return 1;
            }

            var phase = ReadValue("Platoon!A2");
            if (phase != null && phase.Length > 0)
            {
                return Convert.ToInt32(phase);
            }

            return 1;
        }

        /// <summary>
        /// Write a list of units to the Platoons tab
        /// </summary>
        /// <param name="names"></param>
        /// <param name="zone"></param>
        /// <param name="platoon"></param>
        public void Write(List<string> names, int zone, int platoon)
        {
            if (!IsInitialized())
            {
                return;
            }

            // figure out the range
            var col = "";
            switch (platoon)
            {
                case 2:
                    col = "H";
                    break;
                case 3:
                    col = "L";
                    break;
                case 4:
                    col = "P";
                    break;
                case 5:
                    col = "T";
                    break;
                case 6:
                    col = "X";
                    break;
                case 1:
                default:
                    col = "D";
                    break;
            }

            // 2, 20, 38
            var row1 = 2 + (Settings.DonationsPerPlatoon + 3) * (zone - 1);
            var row2 = row1 + Settings.DonationsPerPlatoon - 1;
            var range = string.Format("Platoon!{0}{1}:{2}{3}", col, row1, col, row2);

            // write the data
            WriteValues(range, names);
        }

        /// <summary>
        /// Read the first value from a range
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public string ReadValue(string range)
        {
            if (!IsInitialized())
            {
                return string.Empty;
            }

            // define request parameters
            var request = Service.Spreadsheets.Values.Get(Settings.SpreadsheetID, range);

            // get the data
            var response = request.Execute();
            IList<IList<Object>> values = response.Values;
            if (values != null && values.Count == 1)
            {
                var row = values[0];
                var value = row[0];
                return value.ToString();
            }

            return string.Empty;
        }

        /// <summary>
        /// Read a list of values from a range
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public List<string> ReadValues(string range)
        {
            List<string> results = new List<string>();
            if (!IsInitialized())
            {
                return results;
            }

            // define request parameters
            var request = Service.Spreadsheets.Values.Get(Settings.SpreadsheetID, range);

            // get the data
            var response = request.Execute();
            IList<IList<Object>> values = response.Values;
            if (values != null && values.Count > 0)
            {
                foreach (var row in values)
                {
                    results.Add(row[0].ToString());
                }
            }

            return results;
        }

        /// <summary>
        /// Write a single value to a range
        /// </summary>
        /// <param name="range"></param>
        /// <param name="value"></param>
        public void WriteValue(string range, string value)
        {
            if (!IsInitialized())
            {
                return;
            }

            // prepare the data
            var data = new ValueRange();
            var oblist = new List<object>() { value };
            data.Values = new List<IList<object>> { oblist };

            Send(data, range);
        }

        /// <summary>
        /// Write a list of values to a range (column only)
        /// </summary>
        /// <param name="range"></param>
        /// <param name="values"></param>
        public void WriteValues(string range, List<string> values)
        {
            if (!IsInitialized())
            {
                return;
            }

            // prepare the data
            var data = new ValueRange();
            data.MajorDimension = "COLUMNS";
            var oblist = new List<object>();
            foreach (var value in values)
            {
                oblist.Add(value);
            }
            data.Values = new List<IList<object>> { oblist };

            Send(data, range);
        }

        /// <summary>
        /// Send changes to Google Sheets
        /// </summary>
        /// <param name="data"></param>
        /// <param name="range"></param>
        private void Send(ValueRange data, string range)
        {
            if (!IsInitialized())
            {
                return;
            }

            // send to Sheets
            var update = Service.Spreadsheets.Values.Update(data, Settings.SpreadsheetID, range);
            update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            var result = update.Execute();
        }
    }
}
