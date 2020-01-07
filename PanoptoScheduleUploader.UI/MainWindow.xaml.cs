using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PanoptoScheduleUploader.Core;
using System.Configuration;
using System.Threading;
using System.Data;
using System.ComponentModel;
using PanoptoScheduleUploader.Services;
using System.Diagnostics;

namespace PanoptoScheduleUploader.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private const string SCHEDULE_LIST_PATH = @"/Panopto/Pages/Sessions/List.aspx#status=%5B1%5D";
        private const string NUMBER = "Number";
        private const string TITLE = "Title";
        private const string RECORDING_DATE = "Recording Date";
        private const string START_TIME = "Start Time";
        private const string END_TIME = "End Time";
        private const string PRESENTER = "Presenter";
        private const string FOLDER = "Folder";

        private IEnumerable<Services.SchedulingResult> results = null;
        private Dictionary<Services.SessionManagement.Session, SessionUsage> sessions = null;

        public MainWindow()
        {
            InitializeComponent();
            previewGrid.Visibility = System.Windows.Visibility.Hidden;
        }

        private void openButton_Click(object sender, RoutedEventArgs e)
        {
            resultsTextBlock.Text = "";
            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = ""; // Default file name
            dlg.DefaultExt = ".xml"; // Default file extension
            dlg.Filter = "XML or CSV documents (.xml, .csv)|*.xml;*.csv"; // Filter files by extension 

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results 
            if (result == true)
            {
                // Open document 
                fileInput.Text = dlg.FileName;
                previewGrid.Visibility = System.Windows.Visibility.Visible;
                resultsContainer.Visibility = Visibility.Hidden;
                try
                {
                    previewGrid.DataContext = PreviewTable(fileInput.Text);
                }
                catch (Exception ex)
                {
                    log.Warn("An error occurred.", ex);
                    MessageBox.Show("An error occurred. Details: " + ex.Message);
                    fileInput.Text = "";
                }
                
                //Set the column widths to make it look prettier 
                previewGrid.Columns[0].Width = 100;
                previewGrid.Columns[1].Width = 200;
                previewGrid.Columns[2].Width = 100;
                previewGrid.Columns[3].Width = 100;
                previewGrid.Columns[4].Width = 150;
                previewGrid.Columns[5].Width = 142;
            }
        }

        private DataView PreviewTable(string fileName)
        {
            DataTable table = new DataTable();
            int lineNumber = 0;
            try
            {
                table.Columns.Add(NUMBER);
                table.Columns.Add(TITLE);
                table.Columns.Add(RECORDING_DATE);
                table.Columns.Add(START_TIME);
                table.Columns.Add(END_TIME);
                table.Columns.Add(PRESENTER);
                table.Columns.Add(FOLDER);

                IEnumerable<Recording> recordings = null;
                if (System.IO.Path.GetExtension(fileName) == ".xml")
                {
                    var parser = new RecorderScheduleXmlParser(fileName);
                    recordings = parser.ExtractRecordings();
                }
                else if (System.IO.Path.GetExtension(fileName) == ".csv")
                {
                    var parser = new RecorderScheduleCSVParser(fileName);
                    recordings = parser.ExtractRecordings();
                }
                lineNumber++;

                int rowCount = 0;
                foreach (var recording in recordings)
                {
                    var row = table.NewRow();
                    rowCount++; row[NUMBER] = rowCount;
                    row[TITLE] = recording.Title;
                    row[START_TIME] = recording.StartTime.ToShortTimeString();
                    row[END_TIME] = recording.EndTime.ToShortTimeString();
                    row[PRESENTER] = recording.Presenter;
                    row[FOLDER] = recording.CourseTitle;
                    row[RECORDING_DATE] = recording.RecordingDate;
                    table.Rows.Add(row);
                }
            }
            catch (Exception ex)
            {
                log.Warn(ex);
                MessageBox.Show(ex.Message);
            }

            return table.AsDataView();
        }

        private void submitButton_Click(object sender, RoutedEventArgs e)
        {
            previewGrid.Visibility = System.Windows.Visibility.Hidden;
            resultsContainer.Visibility = Visibility.Visible;
            resultsTextBlock.Text = "";
            submitButton.IsEnabled = false;

            var username = usernameInput.Text;
            var password = passwordInput.Password;
            var filePath = fileInput.Text;
            bool overwrite = chkOverwrite.IsChecked.Value;
            

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(filePath))
            {
                MessageBox.Show("All fields must be populated.");
                return;
            }

            try
            {
                var backgroundWorker = new BackgroundWorker();

                backgroundWorker.DoWork += (s, e1) =>
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        resultsTextBlock.Text = "Batch creating your sessions. This may take a few minutes, please be patient.";
                    });

                    try
                    {
                        //results = SetRecordingSchedules.Execute(username, password, filePath, string.Empty, overwrite);
                        results = SetRecordingSchedules.FixSchedule(username, password, filePath, string.Empty, overwrite);
                    }
                    catch (Exception ex)
                    {
                        log.Warn("An error occurred.", ex);
                        MessageBox.Show("An error occurred. Details: " + ex.Message);
                    }
                };

                backgroundWorker.RunWorkerCompleted += (s, e2) =>
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        resultsTextBlock.Text = string.Empty;

                        if (results != null)
                        {
                            foreach (var result in results)
                            {
                                resultsTextBlock.Text += result.Result;
                                resultsTextBlock.Text += "\r\n\r\n";
                            }
                            resultsTextBlock.Text += string.Format("{0} out of {1} recordings were scheduled.", results.Count(r => r.Success), results.Count());
                            resultsTextBlock.Text += "\r\n\r\n";

                            var targetServername = ConfigurationManager.AppSettings["TargetServerName"];
                            if (targetServername != null)
                            {
                                Uri targetServerUri = new Uri(targetServername);
                                Uri scheduleListUri = new Uri(targetServerUri, SCHEDULE_LIST_PATH);

                                Run scheduleListUrl = new Run("Scheduled Recording List");
                                Hyperlink scheduleHyperlink = new Hyperlink(scheduleListUrl)
                                {
                                    NavigateUri = scheduleListUri
                                };
                                scheduleHyperlink.RequestNavigate += new System.Windows.Navigation.RequestNavigateEventHandler(Hyperlink_RequestNavigate);
                                resultsTextBlock.Inlines.Add(scheduleHyperlink);
                            }

                        }
                        else
                        {
                            log.Warn("Unable to process file");
                            MessageBox.Show("Unable to process file");
                        }

                        submitButton.IsEnabled = true;
                    });   
                };

                backgroundWorker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                log.Warn(ex);
                MessageBox.Show(string.Format("An error has occurred. Details: {0}", ex.Message));
                submitButton.IsEnabled = true;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            var username = usernameInput.Text;
            var password = passwordInput.Password;

            List<Guid> guids = new List<Guid>();
            List<ListBoxItem> itemsToRemove = new List<ListBoxItem>();

            try
            {

                var backgroundWorker = new BackgroundWorker();

                backgroundWorker.DoWork += (s, e1) =>
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        if (deleteView.Items != null)
                        {
                            foreach (ListBoxItem item in deleteView.Items)
                            {
                                if (item != null && item.Content is StackPanel)
                                {
                                    StackPanel sp = (StackPanel)item.Content;
                                    CheckBox check = (CheckBox)sp.Children[0];
                                    if (check.IsChecked.HasValue && check.IsChecked.Value)
                                    {
                                        guids.Add(new Guid(check.Tag.ToString()));
                                        itemsToRemove.Add(item);
                                    }
                                }
                            }
                        }
                        if (guids.Count > 0)
                        {
                            BulkDeleteRecordingSchedules.DeleteSessions(username, password, guids.ToArray());
                        }

                        if (itemsToRemove.Count > 0)
                        {
                            foreach (ListBoxItem item in itemsToRemove)
                            {
                                if (item != null)
                                {
                                    deleteView.Items.Remove(item);
                                }
                            }
                        }
                    });
                };

                backgroundWorker.RunWorkerCompleted += (s, e2) =>
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        Status.Content = guids.Count + " session(s) have been deleted";
                    });
                };

                backgroundWorker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                log.Warn(ex);
                Status.Content = string.Format("An error has occurred. Details: {0}", ex.Message);
            }
        }

        private void searchButton_Click(object sender, RoutedEventArgs e)
        {
            var username = usernameInput.Text;
            var password = passwordInput.Password;
            var to = ToDate.Text;
            var from = FromDate.Text;
            var visitors = Visitors.Text;
            var mins = MinsViewed.Text;
            var views = Views.Text;

            if (string.IsNullOrWhiteSpace(to) || string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(visitors) || string.IsNullOrWhiteSpace(views) || string.IsNullOrWhiteSpace(mins))
            {
                MessageBox.Show(Application.Current.MainWindow, "All fields must be populated.");
                return;
            }

            deleteView.Items.Clear();
            deleteView.Items.Add(new ListBoxItem() { Content = new TextBlock() { Text = "Batch loading your sessions. This may take a few minutes, please be patient." } });

            try
            {

                var backgroundWorker = new BackgroundWorker();

                backgroundWorker.DoWork += (s, e1) =>
                {
                    try
                    {
                        sessions = BulkDeleteRecordingSchedules.FetchSessions(username, password, new Services.SessionFilter()
                        {
                            StartDate = Convert.ToDateTime(from),
                            EndDate = Convert.ToDateTime(to),
                            MinutesViewed = Convert.ToDouble(mins),
                            NumberOfViews = Convert.ToDouble(views),
                            NumberOfVisitors = Convert.ToDouble(visitors)
                        });
                    }
                    catch (Exception ex)
                    {
                        log.Warn(ex);
                        MessageBox.Show("An error occurred. Details: " + ex.Message);
                    }
                };

                backgroundWorker.RunWorkerCompleted += (s, e2) =>
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        deleteView.Items.Clear();

                        if (sessions != null && sessions.Count > 0)
                        {
                            foreach (var session in sessions)
                            {
                                StackPanel panel = new StackPanel();
                                panel.Orientation = Orientation.Horizontal;
                                panel.Margin = new Thickness(5, 3, 5, 3);
                                panel.ToolTip = session.Key.Name;

                                CheckBox check = new CheckBox();
                                check.Tag = session.Key.Id.ToString();
                                check.Width = 25;
                                panel.Children.Add(check);

                                TextBlock text = new TextBlock();
                                text.Width = 260;
                                text.TextWrapping = TextWrapping.NoWrap;
                                text.Text = session.Key.Name;
                                panel.Children.Add(text);

                                TextBlock textFolder = new TextBlock();
                                textFolder.Width = 200;
                                textFolder.TextWrapping = TextWrapping.NoWrap;
                                textFolder.Text = session.Key.FolderName;
                                panel.Children.Add(textFolder);

                                TextBlock textDate = new TextBlock();
                                textDate.Width = 140;
                                textDate.TextWrapping = TextWrapping.NoWrap;
                                textDate.Text = session.Key.StartTime.ToString();
                                panel.Children.Add(textDate);

                                TextBlock noViewsText = new TextBlock();
                                noViewsText.Margin = new Thickness(20, 0, 0, 0);
                                noViewsText.Width = 75;
                                noViewsText.Text = session.Value.NumberOfViews.ToString();
                                panel.Children.Add(noViewsText);

                                TextBlock minsText = new TextBlock();
                                minsText.Margin = new Thickness(5, 0, 0, 0);
                                minsText.Width = 75;
                                minsText.Text = String.Format("{0:0.00}", session.Value.MinutesViewed);
                                panel.Children.Add(minsText);

                                TextBlock visitorsText = new TextBlock();
                                visitorsText.Margin = new Thickness(5, 0, 0, 0);
                                visitorsText.Width = 25;
                                visitorsText.Text = session.Value.NumberOfVisitors.ToString();
                                panel.Children.Add(visitorsText);

                                ListViewItem item = new ListViewItem();
                                item.Content = panel;

                                deleteView.Items.Add(item);
                            }
                        }
                        else
                        {
                            deleteView.Items.Add(new ListBoxItem() { Content = new TextBlock() { Text = "No Results" } });
                        }
                    });
                };

                backgroundWorker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                log.Warn(ex);
                MessageBox.Show(string.Format("An error has occurred. Details: {0}", ex.Message));
            }
        }

        private void SelectAll_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox selectAll = (CheckBox)sender;
            bool isChecked = selectAll.IsChecked.Value;
            if (deleteView.Items != null)
            {
                foreach (ListBoxItem item in deleteView.Items)
                {
                    if (item != null && item.Content is StackPanel)
                    {
                        StackPanel sp = (StackPanel)item.Content;
                        CheckBox check = (CheckBox)sp.Children[0];
                        check.IsChecked = isChecked;
                    }
                }
            }
        }

        private void verifyLogin_Click(object sender, RoutedEventArgs e)
        {
            var username = usernameInput.Text;
            var password = passwordInput.Password;
            using (var remoteRecorderService = new RemoteRecorderManagementWrapper(username, password))
            {
                switch (remoteRecorderService.getListRecordersForLoginVerification())
                {
                    case LoginResults.Failed:
                        MessageBox.Show("Sign-in failed");
                        break;
                    case LoginResults.NoAccess:
                        MessageBox.Show("User has no Remote Recorder Access");
                        break;
                    case LoginResults.Succeeded:
                        MessageBox.Show("Sign-in succeeded");
                        break;
                    case LoginResults.Unknown:
                    default:
                        MessageBox.Show("An error occured while authenticating");
                        break;
                }
            }
        }
    }
}
