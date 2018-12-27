
/////////////////////////////////////////////////////////////////////
// MainWindow.xaml.cs - GUI Functionality is handled               //
// ver 1.0                                                         //
//Author- Sharath Nagendra                                         //
//Source- Jim Fawcett                                              //
//CSE687 - Object Oriented Design, Spring 2018                     //
/////////////////////////////////////////////////////////////////////
/*
* File Operations:
* -------------------
* This package provides functionality for the following objects: 
* - Dropdown box selection handlers
* - All Button clicks
* - Listbox for Files and directories
* 
* Public Interface:
* ---------------------
* processMessgs() - recieve messgs and invokes the received message     
* clearDirectories()- Directory listbox is cleared
* insrtParent()- Navigated to the parent directory
* addDirectories()- Items are added to the directory listbox
* clrFiles()- All the items in the listbox is cleared
* addFile()- Items to file listbox are added
* addClientProcess()- enq the processes
* DispatcherLoadGetDirectories()- Data from server to directory listbox is loaded
* DispatcherLoadGetFiles()- Data from server to file listbox is loaded
* DispatcherReadFile() - Data from the file is read
* DispatcherLoadMakeConnection() - Connection function is handled
* DispatcherLoadGetFile()- single file from server is retrieved 
* DispatcherAcknowledgementCheckin()- Acknowledges checkin
* DispatcherAcknowledgementGetVersion()- Available version of the file from server is retrieved
* DispatcherAcknowledgementViewmeta()- Meta data of a particular file is retrieved 
* DescriptionEnumerator()- Description of a file is retrieved
* DateEnumerator() - Date of creation of a file is retrieved 
* PathEnumerator()- Path of a file is retrieved 
* StatusEnumerator()- Status of a file is retrieved 
* DispatcherAcknowledgementCheckout()- Checkout is acknowledged
* showFile(string fileName, string fileContent)- Content of the file is opened in a new window

* ListDirectoriesOnDoubleClick(object sender, MouseButtonEventArgs e)- Double click on dir list is handled 
* ClickOnConnectButton(object sender, RoutedEventArgs e)- Click on conect button is handled 
* ClickOnCheckinTab(object sender, MouseButtonEventArgs e)- Click on checkin button is handled 
* BrowseOnMouseLeftButtonClick(object sender, MouseButtonEventArgs e)- Click on browse button is handled 
* MouseDoubleClickOnDirList(object sender, MouseButtonEventArgs e)- Double click on checkin directory list is handled
* ClickOnCheckinButton(object sender, RoutedEventArgs e)- Handles checkin button
* ClickOnCheckoutButton(object sender, RoutedEventArgs e)- Handles checkout button
* BrowseFileRadioButton(object sender, RoutedEventArgs e)- Handles radio button to select file to checkin
* AddDependRadioButton(object sender, RoutedEventArgs e)- Handles radio button to add dependency 
* AddCategoryRadioButton(object sender, RoutedEventArgs e)- Handles radio button to add category 

* Required Files:
* ---------------
* MainWindow.xaml
* 

* Maintenance History:
* --------------------
*Ver 1.0 : 04/17/2018
*  - first release
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Collections.ObjectModel;
using System.IO;
using MsgPassingCommunication;
namespace GUI
{
    
    //The following code gives Interaction logic for Mainwindow
  
    public partial class MainWindow : Window
    {
        public class BoolStringClass
        {
            public string LBTxt { get; set; }
            public bool LBSltd { get; set; }
            public bool ListDependency { get; set; }
        }



        public ObservableCollection<BoolStringClass> ListOfFileB { get; set; }
        public MainWindow()
        {
            ListOfFileB = new ObservableCollection<BoolStringClass>();
            InitializeComponent();
            this.DataContext = this;
        }

        
        private List<String> categoryList = new List<string>();
        private string checkinFileCategory="C";
        private string checkinFileDependency="NoSqlDb::FileSysytem.h";
        private string checkinStatus;
        private FileDisplayWin fileWin = null;
        private Stack<string> pathStack_ = new Stack<string>();
        private Translater translater;
        private CsEndPoint endPoint_;
        private Thread rcvThread = null;
        private Dictionary<string, Action<CsMessage>> dispatcher_
          = new Dictionary<string, Action<CsMessage>>();
        private bool connecFlag = false;
        private int windowCond = 0;
        private string CheckinfileName;
        public ObservableCollection<string> Items { get; } = new ObservableCollection<string>();

        // recieve messgs and invokes the received message 

        private void processMessgs()
        {
            ThreadStart thredProcess = () => {
                while (true)
                {
                    CsMessage messgs = translater.getMessage();
                    string messgId = messgs.value("command");
                    if (dispatcher_.ContainsKey(messgId))
                        dispatcher_[messgId].Invoke(messgs);
                }
            };
            rcvThread = new Thread(thredProcess);
            rcvThread.IsBackground = true;
            rcvThread.Start();
        }
        // Directory listbox is cleared

        private void clearDirectories()
        {
            if (windowCond == 0)
            {
                DirList.Items.Clear();
            }
            else { ciDirListBox.Items.Clear(); }
        }
        // Items are added to the directory listbox

        private void addDirectories(string dir)
        {
            if (windowCond == 0)
            {
                DirList.Items.Add(dir);
            }
            else { ciDirListBox.Items.Add(dir); }
        }
        // Navigated to the parent directory

        private void insrtParent()
        {
            if (windowCond == 0)
            {
                DirList.Items.Insert(0, "..");
            }
            else { ciDirListBox.Items.Insert(0, ".."); }
        }
        // All the items in the listbox is cleared

        private void clrFiles()
        {
            if (windowCond == 0)
            {
                FileList.Items.Clear();
            }
            else { ListOfFileB.Clear(); }
        }
        // Items to file listbox are added

        private void addFile(string file)
        {
            if (windowCond == 0)
            {
                FileList.Items.Add(file);
            }
            else { ListOfFileB.Add(new BoolStringClass { LBSltd = false, LBTxt = file, ListDependency = false }); }
        }
        // add client processing for message with key 

        private void addClientProcess(string key, Action<CsMessage> clientProc)
        {
            dispatcher_[key] = clientProc;
        }
        // Data from server to directory listbox is loaded

        private void DispatcherLoadGetDirectories()
        {
            Action<CsMessage> getDirs = (CsMessage rcvMsg) =>
            {
                Action clrDirs = () =>
                {
                    clearDirectories();
                };
                Dispatcher.Invoke(clrDirs, new Object[] { });
                var enumer = rcvMsg.attributes.GetEnumerator();
                while (enumer.MoveNext())
                {
                    string key = enumer.Current.Key;
                    if (key.Contains("dir"))
                    {
                        Action<string> doDir = (string dir) =>
                        {
                            addDirectories(dir);
                        };
                        Dispatcher.Invoke(doDir, new Object[] { enumer.Current.Value });
                    }
                }
                Action insertUp = () =>
                {
                    insrtParent();
                };
                Dispatcher.Invoke(insertUp, new Object[] { });
            };
            Action<CsMessage> getAuthor = (CsMessage rcvMsg) =>
            {
                var enumer = rcvMsg.attributes.GetEnumerator();
                while (enumer.MoveNext())
                {
                    string key = enumer.Current.Key;
                    if (key.Contains("Author"))
                    {
                        Action<string> doDir = (string dir) =>
                        {
                            addDirectories(dir);
                        };
                        Dispatcher.Invoke(doDir, new Object[] { enumer.Current.Value });
                    }
                }
            };
            addClientProcess("getDirs", getDirs);
            addClientProcess("getAuthor", getAuthor);
        }
        // Data from server to file listbox is loaded

        private void DispatcherLoadGetFiles()
        {
            Action<CsMessage> getFiles = (CsMessage rcvMsg) =>
            {
                Action clrFiles = () =>
                {
                    this.clrFiles();
                };
                Dispatcher.Invoke(clrFiles, new Object[] { });
                var enumer = rcvMsg.attributes.GetEnumerator();
                while (enumer.MoveNext())
                {
                    string key = enumer.Current.Key;
                    if (key.Contains("file"))
                    {
                        Action<string> doFile = (string file) =>
                        {
                            addFile(file);
                        };
                        Dispatcher.Invoke(doFile, new Object[] { enumer.Current.Value });
                    }
                }
            };
            addClientProcess("getFiles", getFiles);
        }

        // Data from the file is read
        private void DispatcherReadFile()
        {
            Action<CsMessage> readFile = (CsMessage rcvMsg) =>
            {
                var enumerator = rcvMsg.attributes.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    string key = enumerator.Current.Key;
                    if (key.Contains("content"))
                    {
                        Action<string> mess = (string value) =>
                        {
                            fileWin.FileSpace.Text = enumerator.Current.Value;
                        };
                        Dispatcher.Invoke(mess, new Object[] { enumerator.Current.Value });
                    }
                    if (key.Contains("name"))
                    {
                        Action<string> mess = (string value) =>
                        {
                            fileWin.Title = enumerator.Current.Value;
                        };
                        Dispatcher.Invoke(mess, new Object[] { enumerator.Current.Value });
                    }
                }
            };
            addClientProcess("readFile", readFile);
        }
        // Connection function is handled

        private void DispatcherLoadMakeConnection()
        {
            Action<CsMessage> connection = (CsMessage rcvMsg) =>
            {
                var enumerator = rcvMsg.attributes.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    string key = enumerator.Current.Key;
                    if (key.Contains("message"))
                    {
                        Action<string> connect = (string status) =>
                        {
                            if (status == "Connected")
                            {
                                connecFlag = true;
                                this.statusBarText.Text = "Connected..";
                            }
                        };
                        Dispatcher.Invoke(connect, new Object[] { enumerator.Current.Value });
                    }
                }
            };
            addClientProcess("connection", connection);
        }

        //Single file from server is retrieved 
        private void DispatcherLoadGetFile()
        {
            Action<CsMessage> getFile = (CsMessage rcvMsg) =>
            {
                Action displayFile = () =>
                {
                    string file_Name =  rcvMsg.value("fileName");
                    string file_Content = System.IO.File.ReadAllText("../../../CppCommWithFileXfer/SaveFiles" + "/" + file_Name);
                    showFile(file_Name, file_Content);
                };
                Dispatcher.Invoke(displayFile, new Object[] { });

                Console.WriteLine("File sent message has been received ");
            };
            addClientProcess("filesent", getFile);
        }

        // Checkin function is loaded and Acknowledges checkin 
        private void DispatcherAcknowledgementCheckin() {

            Action<CsMessage> acknowledgeCheckin = (CsMessage rcvMsg) => {

                var enumerator = rcvMsg.attributes.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    string key = enumerator.Current.Key;
                    if (key.Contains("message"))
                    {
                        Action<string> connect = (string status) =>
                        {
                            if (status == "Checkindone")
                            {
                             
                                this.statusBarText.Text = " Check-In of file is successfull";
                            }
                        };
                        Dispatcher.Invoke(connect, new Object[] { enumerator.Current.Value });
                    }
                }

            };
            addClientProcess("CheckinAcknowledgement", acknowledgeCheckin);

        }


        // Available version of the file from server is retrieved
        private void DispatcherAcknowledgementGetVersion()
        {

            Action<CsMessage> acknowledgeGetVersion = (CsMessage rcvMsg) => {

                var enumerator = rcvMsg.attributes.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    string key = enumerator.Current.Key;
                    if (key.Contains("message"))
                    {
                        Action<string> connect = (string status) =>
                        {
                            VM_Ver_CB.Items.Clear();
                            for (int i = 0; i < Int32.Parse(status); i++) {

                                VM_Ver_CB.Items.Add(i+1);
                            }
                        };
                        Dispatcher.Invoke(connect, new Object[] { enumerator.Current.Value });
                    }
                }
            };
            addClientProcess("VersionAcknowledgement", acknowledgeGetVersion);
        }


        // Meta data of a particular file is retrieved 
        private void DispatcherAcknowledgementViewmeta()
        {

            Action<CsMessage> AcknowledgeViewMetadata = (CsMessage rcvMsg) => {

                var enumerator = rcvMsg.attributes.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current.Key == "description")
                    {
                        DescriptionEnumerator(enumerator, rcvMsg);
                    }
                    if (enumerator.Current.Key == "date")
                    {
                        DateEnumerator(enumerator,rcvMsg);
                    }
                    if (enumerator.Current.Key == "status")
                    {
                        StatusEnumerator(enumerator,rcvMsg);
                    }
                    if (enumerator.Current.Key == "path")
                    {
                        PathEnumerator(enumerator,rcvMsg);
                    }
                    
                }
            };
            addClientProcess("MetadataAcknowledgement", AcknowledgeViewMetadata);
        }

        // Description of a file is retrieved
        private void DescriptionEnumerator(Dictionary<string,string>.Enumerator enumer,CsMessage msg) {
            Action<string> connect = (string status) =>
            {
                VM_FD_TB.Text = msg.attributes["description"];
            };
            Dispatcher.Invoke(connect, new Object[] { enumer.Current.Value });

        }
        // Date of creation of a file is retrieved 
        private void DateEnumerator(Dictionary<string, string>.Enumerator enumer, CsMessage msg)
        {
            Action<string> connect = (string status) =>
            {
                VM_date_TB.Text = msg.attributes["date"];
            };
            Dispatcher.Invoke(connect, new Object[] { enumer.Current.Value });
        }
        // Path of a file is retrieved 
        private void PathEnumerator(Dictionary<string, string>.Enumerator enumer, CsMessage msg)
        {
            Action<string> connect = (string status) =>
            {
                VM_FP_TB.Text = msg.attributes["path"];
            };
            Dispatcher.Invoke(connect, new Object[] { enumer.Current.Value });
        }
        // Status of a file is retrieved 
        private void StatusEnumerator(Dictionary<string, string>.Enumerator enumer, CsMessage msg)
        {
            Action<string> connect = (string status) =>
            {
                VM_CIS_TB.Text = msg.attributes["status"];
            };
            Dispatcher.Invoke(connect, new Object[] { enumer.Current.Value });
        }


        // Checkout is acknowledged
        private void DispatcherAcknowledgementCheckout()
        {

            Action<CsMessage> ackcheckout = (CsMessage rcvMsg) => {

                var enumerator = rcvMsg.attributes.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    string key = enumerator.Current.Key;
                    if (key.Contains("message"))
                    {
                        Action<string> connect = (string status) =>
                        {
                            if (status == "Checkoutdone")
                            {
                                this.statusBarText.Text = " File is checked out successfully";
                            }
                        };
                        Dispatcher.Invoke(connect, new Object[] { enumerator.Current.Value });
                    }
                }
            };
            addClientProcess("CheckoutAcknowledgement", ackcheckout);
        }

        // Content of the file is opened in a new window
        private void showFile(string fileName, string fileContent)
        {
            Console.WriteLine(fileName.ToString());
            FileDisplayWin p = new FileDisplayWin();
            p.FileSpace.Text = fileContent;
            p.Show();
        }
        // Loads all the dispatcher processes

        private void loadDispatcherBrowse()
        {
            DispatcherLoadGetDirectories();
            DispatcherLoadGetFiles();
        }
        private void DispatcherLoad()
        {
            DispatcherAcknowledgementCheckout();
            DispatcherAcknowledgementGetVersion();
            DispatcherLoadMakeConnection();
            DispatcherAcknowledgementCheckin();
            DispatcherReadFile();
            DispatcherAcknowledgementViewmeta();
            loadDispatcherBrowse();
            DispatcherLoadGetFile();
        }

        // Starts the communication, fills the window display with files and directories 
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            endPoint_ = new CsEndPoint();
            endPoint_.machineAddress = "localhost";
            endPoint_.port = 8082; 
            translater = new Translater();
            translater.listen(endPoint_);
            processMessgs(); // message processing is started 
            DispatcherLoad(); // loads all the dispatcher functions 
            Thread.Sleep(500);
            Automatic();
        }
        
        // removes the name of the first part of the path
        private string removeFirstDirectory(string path)
        {
            string changedPath = path;
            int position = path.IndexOf("/");
            changedPath = path.Substring(position + 1, path.Length - position - 1);
            return changedPath;
        }

        // Double click on directory list is handled 
        private void ListDirectoriesOnDoubleClick(object sender, MouseButtonEventArgs e)
        {
            
            if (DirList.SelectedItem == null)
                return;
            string selectedDirectory = (string)DirList.SelectedItem;
            string path;
            if (selectedDirectory == "..")
            {
                if (pathStack_.Count > 1)  
                    pathStack_.Pop();
                else
                    return;
            }
            else
            {
                path = pathStack_.Peek() + "/" + selectedDirectory;
                pathStack_.Push(path);
            } 
            PathTextBlock.Text = removeFirstDirectory(pathStack_.Peek()); // build message to get dirs and post it
            CsEndPoint serverEndPt = new CsEndPoint();
            serverEndPt.machineAddress = "localhost";
            serverEndPt.port = 8080;
            CsMessage messg = new CsMessage();
            messg.add("to", CsEndPoint.toString(serverEndPt));
            messg.add("from", CsEndPoint.toString(endPoint_));
            messg.add("command", "getDirs");
            messg.add("path", pathStack_.Peek());
            translater.postMessage(messg);        // build message to get files and post it
            messg.remove("command");
            messg.add("command", "getFiles");
            translater.postMessage(messg);
        }

        //Click on conect button is handled 
        private void ClickOnConnectButton(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("connecting to localhost 8080");
            CsEndPoint servEndPt = new CsEndPoint();
            servEndPt.machineAddress = this.IPAddrName.Text;
            servEndPt.port = Int32.Parse(this.PortName.Text);
            CsMessage messgs = new CsMessage();
            messgs.add("to", CsEndPoint.toString(servEndPt));
            messgs.add("from", CsEndPoint.toString(endPoint_));
            messgs.add("command", "connection");
            translater.postMessage(messgs);
            messgs.remove("command");
        }

        //Functionality of checkin tab is handled 
        private void ClickOnCheckinTab(object sender, MouseButtonEventArgs e)
        {
            windowCond = 1;
            dependfinal.Items.Clear();
        }
        private void MouseLeftButtonTab(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("\n Tab to Connect");
        }

        private void MouseLeftButtonTab_1(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("\n Tab to checkout");
        }

        private void MouseLeftButtonTab_2(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("\n Tab to view meta data");
        }

        //Functionality of browse button is handled 
        private void BrowseOnMouseLeftButtonClick(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("\n Tab to browse");
            if (!connecFlag)
                return;
            windowCond = 0;

            CsEndPoint servEndPt = new CsEndPoint();
            servEndPt.machineAddress = "localhost";
            servEndPt.port = 8080;

            PathTextBlock.Text = "Storage";
            pathStack_.Push("../Storage");
            CsMessage messg = new CsMessage();
            messg.add("to", CsEndPoint.toString(servEndPt));
            messg.add("from", CsEndPoint.toString(endPoint_));
            messg.add("command", "getDirs");
            messg.add("path", pathStack_.Peek());
            translater.postMessage(messg);
            messg.remove("command");
            messg.add("command", "getFiles");
            translater.postMessage(messg);
            messg.remove("command");
        }

        // Double click on directory list box is handled 
        private void MouseDoubleClickOnDirList(object sender, MouseButtonEventArgs e) {
            if (ciDirListBox.SelectedItem == null)
                return;
            // build path for selected dir
            string sltdDirectory = (string)ciDirListBox.SelectedItem;
            string path;
            if (sltdDirectory == "..")
            {
                if (pathStack_.Count > 1)  // don't pop off "Storage"
                    pathStack_.Pop();
                else
                    return;
            }
            else
            {
                path = pathStack_.Peek() + "/" + sltdDirectory;
                pathStack_.Push(path);
            }


            // build message to get dirs and post it
            CsEndPoint serverEndPt = new CsEndPoint();
            serverEndPt.machineAddress = "localhost";
            serverEndPt.port = 8080;
            CsMessage messg = new CsMessage();
            messg.add("to", CsEndPoint.toString(serverEndPt));
            messg.add("from", CsEndPoint.toString(endPoint_));
            messg.add("command", "getDirs");
            messg.add("path", pathStack_.Peek());
            translater.postMessage(messg);          
            messg.remove("command"); // build message to get files and post it
            messg.add("command", "getFiles");
            translater.postMessage(messg);
        }

        // Click on checkin button is handled 
        private void ClickOnCheckinButton(object sender, RoutedEventArgs e)
        {
            if (CheckinCheckList())
            {
                Console.WriteLine("File Checkin");
                CsEndPoint serverEndPt = new CsEndPoint();
                serverEndPt.machineAddress = "localhost";
                serverEndPt.port = 8080;
                CsMessage messg = new CsMessage();
                messg.add("to", CsEndPoint.toString(serverEndPt));
                messg.add("from", CsEndPoint.toString(endPoint_));
                messg.add("command", "Checkinfile");
                messg.add("path", SelectFileTB.Text.ToString());
                messg.add("filename", ExtractFilename(SelectFileTB.Text.ToString()));
                messg.add("description", DescripTB.Text.ToString());
                messg.add("status", checkinStatus);
                messg.add("category", checkinFileCategory);
                messg.add("dependency", checkinFileDependency);
                translater.postMessage(messg);
                messg.show();
            }

        }

        // click on checkout button is handled 
        private void ClickOnCheckoutButton(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Checkout file");
            CsEndPoint serverEndPt = new CsEndPoint();
            serverEndPt.machineAddress = "localhost";
            serverEndPt.port = 8080;
            CsMessage messg = new CsMessage();
            messg.add("to", CsEndPoint.toString(serverEndPt));
            messg.add("from", CsEndPoint.toString(endPoint_));
            messg.add("command", "Checkoutfile");
            messg.add("namespace", NamespaceCOTB.Text.ToString());
            messg.add("filename", SelectFileCOTB.Text.ToString());
            messg.add("version",VersionCheckList() );
            
            translater.postMessage(messg);
            messg.show();
        }


        // Handles radio button to select file to checkin
        void BrowseFileRadioButton(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Browse clicked");
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".cpp";
            dlg.Filter = "*.h|*.cpp";
            
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                
                string filename = dlg.FileName;
                SelectFileTB.Text = filename;
            }
        }

        // Handles radio button to add dependency 
        void AddDependRadioButton(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Add Dependcy clicked");
            AddcatButton.IsEnabled = false;
            confirmDependclick.IsEnabled = true;
            dependfinal.Items.Clear();
            dependfinalTB.Text = "Dependencies";
            dependfinal.SelectionMode = SelectionMode.Single;
            if (!connecFlag)
                return;
            windowCond = 1;
            CsEndPoint ServerEndPt = new CsEndPoint();
            ServerEndPt.machineAddress = "localhost";
            ServerEndPt.port = 8080;
            PathTextBlock.Text = "Storage";
            pathStack_.Clear();
            pathStack_.Push("../Storage");
            CsMessage messg = new CsMessage();
            messg.add("to", CsEndPoint.toString(ServerEndPt));
            messg.add("from", CsEndPoint.toString(endPoint_));
            messg.add("command", "getDirs");
            messg.add("path", pathStack_.Peek());
            translater.postMessage(messg);
            messg.remove("command");
            messg.add("command", "getFiles");
            translater.postMessage(messg);
            messg.remove("command");
        }

        // Handles radio button to add category 
        void AddCategoryRadioButton(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("On click of add category");
            AddcatButton.IsEnabled = true;
            confirmDependclick.IsEnabled = false;
            dependfinalTB.Text = "Categories";
            dependfinal.Items.Clear();
            dependfinal.SelectionMode = SelectionMode.Multiple;
            dependfinal.Items.Add("A");
            dependfinal.Items.Add("B");
            dependfinal.Items.Add("C");
            dependfinal.Items.Add("D");
            dependfinal.Items.Add("E");
            dependfinal.Items.Add("F");
        }

        // Add the dependent files from file list to dependency listbox 
        private void AddSelectFileForDep(object sender, RoutedEventArgs e)
        {
            foreach (var files in ListOfFileB)
            {
                if (files.LBSltd == true && files.ListDependency == false)
                {
                    files.ListDependency = true;
                    dependfinal.Items.Add(files.LBTxt);
                }
                else if (files.LBSltd == false && files.ListDependency == true)
                {
                    files.ListDependency = false;
                    dependfinal.Items.Remove(files.LBTxt);
                }
                dependfinal.Items.Refresh();
            }
        }


        //-Function to get files from list box
        private void GetDepFileFromListbox(object sender, SelectionChangedEventArgs e)
        {
            
            categoryList.Clear();
            if (dependfinal.SelectionMode == SelectionMode.Multiple)
            {
                foreach (var item in dependfinal.SelectedItems) {
                    categoryList.Add(item.ToString());

                }

            }

        }

        // Function to get name of the file from file path
        private string ExtractFilename(string filename)
        {
            CheckinfileName =System.IO.Path.GetFileName(filename);
            return CheckinfileName;
        }

        // Function to add category in the file meta data
        private void AddCategoryToMeta(object sender, RoutedEventArgs e)
        {
            
            if (!String.IsNullOrEmpty(otherCatTB.Text))
            {
                categoryList.Add(SelectFileTB.Text);
            }
            SelectFileTB.Text = categoryList.Count().ToString();
        }

        // Function to select the checkin status of a file
        private void SelectionOfFileCheckinStatus(object sender, SelectionChangedEventArgs e)
        {
            if (((ComboBoxItem)CScombox.SelectedItem).Content.ToString() == "Open") {
                checkinStatus = "Open";
            }
            else if(((ComboBoxItem)CScombox.SelectedItem).Content.ToString() == "Close") {
                checkinStatus = "Close";
            }
        }

        // Function that handles the view meta data button
        private void ClickOnVieMetaButton(object sender, RoutedEventArgs e)
        {
            CsEndPoint serverEndPt = new CsEndPoint();
            serverEndPt.machineAddress = "localhost";
            serverEndPt.port = 8080;
            CsMessage messg = new CsMessage();
            messg.add("to", CsEndPoint.toString(serverEndPt));
            messg.add("from", CsEndPoint.toString(endPoint_));
            messg.add("command", "viewmeta");
            messg.add("namespace", VM_NS_TB.Text.ToString());
            messg.add("filename", VM_FN_TB.Text.ToString());
            messg.add("version", "3");
            translater.postMessage(messg);
        }

        // Function to get the version of the file
        private void GetVersionOnClick(object sender, RoutedEventArgs e)
        {
           VM_Ver_CB.IsEnabled = true;
           
               
            CsEndPoint serverEndPt = new CsEndPoint();
            serverEndPt.machineAddress = "localhost";
            serverEndPt.port = 8080;
            CsMessage messg = new CsMessage();
            messg.add("to", CsEndPoint.toString(serverEndPt));
            messg.add("from", CsEndPoint.toString(endPoint_));
            messg.add("command", "getversion");
            messg.add("namespace", VM_NS_TB.Text.ToString());
            messg.add("filename", VM_FN_TB.Text.ToString());
            translater.postMessage(messg);
        }

        // Function to confirm the dependency selection
        private void ConfirmDependencyOnClick(object sender, RoutedEventArgs e)
        {

        }

        // Function to check whether the textboxs are emprty before checkin 
        private bool CheckinCheckList() {
            if (!string.IsNullOrEmpty(DescripTB.Text) && !string.IsNullOrEmpty(NamespaceTB.Text) && !string.IsNullOrEmpty(SelectFileTB.Text) && !string.IsNullOrEmpty(checkinStatus) && !string.IsNullOrEmpty(checkinFileCategory) && !string.IsNullOrEmpty(checkinFileDependency))
            {
                return true;
            }
            return false; 
        }

        // Function to get the resulting version of file
        private string VersionCheckList() {
            if (string.IsNullOrEmpty(versionTB.Text.ToString())) {
                return "0";
            }
            return versionTB.Text.ToString();
        }

        // Function below is triggered when a version is selected in combobox 
        private void VM_Ver_CB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
           
        }

        // Function to open a file when doubled clicked in browse filelist
        private void ListFileOnDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // path for the selected file being built 
            string selectedFile = pathStack_.Peek() + "/" + (string)DirList.SelectedItem + (string)FileList.SelectedItem;

            Console.WriteLine("++" + selectedFile);

           
            CsEndPoint serverEndPt = new CsEndPoint();
            serverEndPt.machineAddress = "localhost";
            serverEndPt.port = 8080;
            
            CsMessage messg = new CsMessage();
            messg.add("to", CsEndPoint.toString(serverEndPt));
            messg.add("from", CsEndPoint.toString(endPoint_));
            messg.add("command", "getFile");
            messg.add("path", selectedFile);
            translater.postMessage(messg);
        }

        // Function to demostrate the functionalities automatically 
        private void Automatic() {

            Console.WriteLine("\n---------------------------------------\nDemonstrating GUI Connect functionality \n");
            AutomaticConnection();
            Console.WriteLine("\n---------------------------------------\nDemonstrating GUI Checkin functionality \n ");
            AutomaticCheckin();
            Console.WriteLine("\n---------------------------------------\nDemonstrating GUI Checkout functionality \n");
            AutomaticCheckout();
            Console.WriteLine("\n---------------------------------------\nDemonstrating GUI Browse and View File functionality \n ");
            Console.WriteLine("\n \b To browse files the files are loaded in the browse list \nClick on a file to display its contents in a seperate window \n The file socket.h is being displayed");
            AutomaticBrowse();
          
            Console.WriteLine("\n---------------------------------------\nDemonstrating GUI View Meta data functionality \n ");
            AutomaticViewMetadata();


        }

        // Function implemention the automated connect function
        private void AutomaticConnection() {
            Console.WriteLine("Address: localhost Port:8080");
            CsEndPoint serverEndPt = new CsEndPoint();
            serverEndPt.machineAddress = this.IPAddrName.Text;
            serverEndPt.port = Int32.Parse(this.PortName.Text);
            CsMessage messg = new CsMessage();
            messg.add("to", CsEndPoint.toString(serverEndPt));
            messg.add("from", CsEndPoint.toString(endPoint_));
            messg.add("command", "connection");
            translater.postMessage(messg);
            messg.remove("command");
            messg.show();
        }

        // Function implemention the automated checkin function
        private void AutomaticCheckin() {
            Console.WriteLine(" The file Sockets.h of namespace NoSqlDb is being checkedin");
            Console.WriteLine(" Description -- Testing check-in");
            Console.WriteLine(" Path -- C:\\Users\\Sharath Nagendra\\Downloads\\Project3\\Storage\\Sockets.h");
            Console.WriteLine(" Status - Close");
            Console.WriteLine(" Catergory - C | Dependency - NoSqlDb::FileSystem.h");
            NamespaceTB.Text = "NoSqlDb";
            DescripTB.Text = "Testing check-in";
            SelectFileTB.Text = "C:\\Users\\Sharath Nagendra\\Downloads\\Project3\\Storage\\Sockets.h";
            if (CheckinCheckList())
            {
                CsEndPoint serverEndPt = new CsEndPoint();
                serverEndPt.machineAddress = "localhost";
                serverEndPt.port = 8080;
                CsMessage messg = new CsMessage();
                messg.add("to", CsEndPoint.toString(serverEndPt));
                messg.add("from", CsEndPoint.toString(endPoint_));
                messg.add("command", "Checkinfile");
                messg.add("path", SelectFileTB.Text.ToString());
                messg.add("filename", ExtractFilename(SelectFileTB.Text.ToString()));
                messg.add("description", DescripTB.Text.ToString());
                messg.add("status", checkinStatus);
                messg.add("category", checkinFileCategory);
                messg.add("dependency", checkinFileDependency);
                translater.postMessage(messg);
                messg.show();
            }

        }

        //- Function implemention the automated checkout function
        private void AutomaticCheckout() {
            Console.WriteLine("File from namespace NoSqlDb is being checked out");
            Console.WriteLine("File: Sockets.h");
            Console.WriteLine("Version: 3\n");
            NamespaceCOTB.Text = "NoSqlDb";
            SelectFileCOTB.Text = "Sockets.h";
            versionTB.Text = "3";
            CsEndPoint serverEndPt = new CsEndPoint();
            serverEndPt.machineAddress = "localhost";
            serverEndPt.port = 8080;
            CsMessage messg = new CsMessage();
            messg.add("to", CsEndPoint.toString(serverEndPt));
            messg.add("from", CsEndPoint.toString(endPoint_));
            messg.add("command", "Checkoutfile");
            messg.add("namespace", NamespaceCOTB.Text.ToString());
            messg.add("filename", SelectFileCOTB.Text.ToString());
            messg.add("version", VersionCheckList());

            translater.postMessage(messg);
          
            messg.show();
        }

        // Function implemention the automated browse function
        private void AutomaticBrowse() {

            Console.WriteLine("\n The file Comm.h is being selected from filelist \n The contents of the file is being displayed on doouble click \n For this demonstration Comm.h is transfered from ../Storage/Comm.h to ../CppCommWithFileXfer/SaveFiles ");
            string selectedFile = "../Storage/Comm.h";

            CsEndPoint serverEndPt = new CsEndPoint();
            serverEndPt.machineAddress = "localhost";
            serverEndPt.port = 8080;

            CsMessage messg = new CsMessage();
            messg.add("to", CsEndPoint.toString(serverEndPt));
            messg.add("from", CsEndPoint.toString(endPoint_));
            messg.add("command", "getFile");
            messg.add("path", selectedFile);
            translater.postMessage(messg);
            messg.show();
        }

        // Function implemention the automated View Metadata function
        private void AutomaticViewMetadata() {
            Console.WriteLine("Namespace: NoSqlDb");
            Console.WriteLine("File: Sockets.h");

            VM_NS_TB.Text = "NoSqlDb";
            VM_FN_TB.Text = "Sockets.h";
            CsEndPoint serverEndPt = new CsEndPoint();
            serverEndPt.machineAddress = "localhost";
            serverEndPt.port = 8080;
            CsMessage messg = new CsMessage();
            messg.add("to", CsEndPoint.toString(serverEndPt));
            messg.add("from", CsEndPoint.toString(endPoint_));
            messg.add("command", "getversion");
            messg.add("namespace", VM_NS_TB.Text.ToString());
            messg.add("filename", VM_FN_TB.Text.ToString());
            translater.postMessage(messg);
            Console.WriteLine("\n Version of the file is retrieved");
            messg.show();

            Console.WriteLine("Version number 4 is selected");
            Console.WriteLine("Getting Meta Data of 4th version of NoSqlDb::Sockets.h");
            VM_Ver_CB.SelectedIndex = 4;
            
            serverEndPt.machineAddress = "localhost";
            serverEndPt.port = 8080;
            CsMessage messg1 = new CsMessage();
            messg1.add("to", CsEndPoint.toString(serverEndPt));
            messg1.add("from", CsEndPoint.toString(endPoint_));
            messg1.add("command", "viewmeta");
            messg1.add("namespace", VM_NS_TB.Text.ToString());
            messg1.add("filename", VM_FN_TB.Text.ToString());
            messg1.add("version", "4");
            translater.postMessage(messg1);
            messg1.show();
        }      
    }
}
