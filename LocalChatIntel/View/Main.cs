using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using System.Collections;

// TODO: Add proper logging (since I can't do a fully portable app)
namespace LocalChatIntel
{
    /// <summary>
    /// <para>Loci: An Intel tool for pilots to find quick intel on everyone in a chat channel</para>
    /// Usage: Copy any in-game chat member list and paste into the tool
    /// </summary>
    public partial class MainWindow : Form
    {
        private enum Status { Ready, PilotLookup, GroupLookup, StatsLookup };
        
        private LookupController lookup;

        private bool running = false;
        private DataGridViewColumn sortColumn;

        public MainWindow()
        {
            InitializeComponent();
            InitializeControls();

            InitializeDataTable(ref primaryTable);
            InitializeDataGridView(ref primaryTable);

            InitializeMainDisplay();

            lookup = new LookupController();
        }

        /// <summary>
        /// Perform custom initialization of controls
        /// </summary>
        private void InitializeControls()
        {
            mainList.BackgroundColor = Color.FromArgb(51, 58, 59);

            mainList.GridColor = mainList.BackgroundColor;
            mainList.DefaultCellStyle.BackColor = mainList.BackgroundColor;
            mainList.ColumnHeadersDefaultCellStyle.BackColor = mainList.BackgroundColor;
            splitContainer1.BackColor = mainList.BackgroundColor;
            statusStrip.BackColor = mainList.BackgroundColor;
            statusLabel.BackColor = mainList.BackgroundColor;
        }

        /// <summary>
        /// Perform custom initialization of data source for the main display
        /// </summary>
        private void InitializeDataTable(ref DataTable table)
        {
            table = new DataTable();

            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Pilot", typeof(string));
            table.Columns.Add("Corporation", typeof(string));
            table.Columns.Add("Alliance", typeof(string));
            table.Columns.Add("Dangerous", typeof(int));
            table.Columns.Add("Solo", typeof(int));
            table.Columns.Add("Notes", typeof(string));

            table.PrimaryKey = new DataColumn[] { table.Columns["Id"] };
        }

        /// <summary>
        /// Initialize the DataGridView for displaying information
        /// </summary>
        /// <param name="table">The datasource for the DGV</param>
        private void InitializeDataGridView(ref DataTable table)
        {
            mainList.DataSource = table;
            mainList.Columns["Id"].Visible = false;
        }

        /// <summary>
        /// Perform custom initialization of the main display
        /// </summary>
        private void InitializeMainDisplay()
        {
            sortColumn = mainList.Columns["Dangerous"];
            mainList.Columns["Dangerous"].DefaultCellStyle.Format = "0\\%";
            mainList.Columns["Solo"].DefaultCellStyle.Format = "0\\%";

            foreach (DataGridViewColumn column in mainList.Columns)
            {
                column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            }
        }

        /// <summary>
        /// Run the pilot lookup algorithm when data is pasted into the tool
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KeyPressed(object sender, KeyEventArgs e)
        {
            bool PasteDetected = e.Control && e.KeyCode == Keys.V;
            if (PasteDetected && !running)
            {
                primaryTable.Rows.Clear();
                string clipboard = Clipboard.GetText().Trim();
                List<string> names = clipboard.Split(new[] { "\r\n", "\r", "\n", }, StringSplitOptions.None).ToList();
                List<string> prunedList = names.Take(999).ToList();

                running = true;
                Task lookup = StartLookup(prunedList);
            }
        }

        /// <summary>
        /// Open zkillboard to the character's page when a row is double clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RowDoubleClicked(object sender, DataGridViewCellEventArgs e)
        {
            DataGridViewRow row = mainList.Rows[e.RowIndex];
            string id = row.Cells["Id"].Value.ToString();
            string url = "https://zkillboard.com/character/" + id + "/";
            System.Diagnostics.Process.Start(url);
        }

        /// <summary>
        /// Overall pilot lookup algorithm run whenever a paste is detected
        /// </summary>
        /// <param name="names">A list of names</param>
        private async Task StartLookup(List<string> names)
        {
            try
            {
                SetStatus(Status.PilotLookup);
                List<Row> cachedRows = lookup.FromCache(ref names);
                UpdateUI(cachedRows);

                if (names.Count > 0)
                {
                    List<PilotId> pilots = await lookup.GetIds(names);
                    UpdateUI(pilots);

                    SetStatus(Status.GroupLookup);
                    List<Affiliation> affiliations = await lookup.GetAffiliations(pilots);
                    UpdateUI(affiliations);

                    SetStatus(Status.StatsLookup);
                    foreach (PilotId id in pilots)
                    {
                        Affiliation affiliation = affiliations.Find(x => x.Character_Id == id.Id);
                        PilotStats stats = await lookup.FindStatsFor(affiliation); // affiliation already has pilot id
                        UpdateUI(stats);
                        RefreshSorting();
                    }
                }

                SetStatus(Status.Ready);
                running = false;
            }
            catch (HttpRequestException hre)
            {
                Console.WriteLine(hre.Message);
                statusLabel.Text = hre.Message;
            }
        }

        /// <summary>
        /// Update the UI with a list of rows
        /// </summary>
        /// <param name="rows"></param>
        private void UpdateUI(List<Row> rows)
        {
            foreach (Row row in rows)
            {
                DataRow dataRow = primaryTable.NewRow();
                dataRow["Id"] = row.Pilot_Id;
                dataRow["Pilot"] = row.Pilot_Name;
                dataRow["Corporation"] = row.Corp_Name;
                dataRow["Alliance"] = row.Alliance_Name;
                dataRow["Dangerous"] = row.Danger_Percent;
                dataRow["Solo"] = row.Solo_Percent;
                dataRow["Notes"] = row.Notes;

                primaryTable.Rows.Add(dataRow);
                RefreshSorting();
            }
        }

        /// <summary>
        /// Update UI with a list of pilots
        /// </summary>
        /// <param name="ids"></param>
        private void UpdateUI(List<PilotId> ids)
        {
            DataRow row;
            foreach (PilotId id in ids.ToList())
            {
                row = primaryTable.NewRow();
                row["Id"] = id.Id;
                row["Pilot"] = id.Name;
                primaryTable.Rows.Add(row);
                RefreshSorting();
            }
        }

        /// <summary>
        /// Update UI with a list of pilot affiliations
        /// </summary>
        /// <param name="pilots"></param>
        private void UpdateUI(List<Affiliation> pilots)
        {
            foreach (Affiliation pilot in pilots.ToList())
            {
                DataRow row = primaryTable.Rows.Find(pilot.Character_Id);
                row["Corporation"] = pilot.Corporation;
                row["Alliance"] = pilot.Alliance;
                RefreshSorting();
            }

        }

        /// <summary>
        /// Update UI with an individual pilot's statistics
        /// </summary>
        /// <param name="pilotStats"></param>
        private void UpdateUI(PilotStats pilotStats)
        {
            DataRow row = primaryTable.Rows.Find(pilotStats.PilotId);
            row["Dangerous"] = pilotStats.DangerPercent;
            row["Solo"] = pilotStats.SoloPercent;
            row["Notes"] = lookup.GetNotesFor(pilotStats);

            RefreshSorting();
        }

        /// <summary>
        /// Refresh data grid view sorting
        /// </summary>
        private void RefreshSorting()
        {
            DataGridViewColumn sort = mainList.SortedColumn ?? mainList.Columns[sortColumn.Name];

            mainList.Sort(sort, ListSortDirection.Descending);
            mainList.ClearSelection();
            if (mainList.Rows.Count > 0) mainList.Rows[0].Selected = true;
            mainList.Refresh();
        }

        /// <summary>
        /// Update the status label text at the bottom of the window
        /// </summary>
        /// <param name="status">A Status enum</param>
        private void SetStatus(Status status)
        {
            switch (status)
            {
                case Status.Ready:
                    statusLabel.Text = "Ready";
                    break;
                case Status.PilotLookup:
                    statusLabel.Text = "Retrieving pilot info...";
                    break;
                case Status.GroupLookup:
                    statusLabel.Text = "Finding pilot groups...";
                    break;
                case Status.StatsLookup:
                    statusLabel.Text = "Scraping killboards...";
                    break;
                default:
                    statusLabel.Text = "";
                    break;
            }
        }
    }
}
