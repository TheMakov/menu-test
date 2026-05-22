using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using UnityEngine;
using Debug = UnityEngine.Debug;

public enum CardType
{
    GameLauncher,
    ShutdownButton,
}

public class Card: MonoBehaviour
{
    [SerializeField] private CardType type;
    [SerializeField] private string path;
    [SerializeField] private string execName;
        
    public bool isSelected = false;

    private Animator _animator;
    private static readonly int IsSelected = Animator.StringToHash("isSelected");
    private ArduinoTest _controler;
    
    private void Start()
    {
        _controler = FindObjectOfType<ArduinoTest>();
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        _animator.SetBool(IsSelected, isSelected);
    }
        
    public void LaunchGame()
    {
        if (type == CardType.GameLauncher)
        {
            if (execName.Length >= 3 )
            {
                var FileType = execName.Substring(execName.Length-3, 3);
                var rootDir = Directory.GetCurrentDirectory();
                _controler.ClosePort();
                _controler.gameIsRunning = true;
                StartCoroutine(WaitAndInitialise(FileType, rootDir));
            }
        }
        else if (type == CardType.ShutdownButton)
        {
            UnityEngine.Debug.Log("the console is shutting down");
            Application.Quit(0);
        }
    }

    IEnumerator WaitAndInitialise(string FileType, string rootDir)
    {
        yield return new WaitForSeconds(0.5f); // Give Windows time to breathe
        if (FileType == "jar")
        {
            string fullJarPath = $"{rootDir}{path}{execName}";
            string jarFolder = Path.GetDirectoryName(fullJarPath);
            string batPath = Path.Combine(jarFolder, "run_pong.bat");

            // 2. Create the exact commands you typed manually
            string[] batContent = new string[] {
                $"cd /d \"{jarFolder}\"",
                $"java -jar \"{fullJarPath}\"",
                "exit"
            };

            // 3. Write the batch file to the disk
            File.WriteAllLines(batPath, batContent);
            Debug.Log("Batch file created at: " + batPath);

            // 4. Launch the batch file directly
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "explorer.exe";
            startInfo.WorkingDirectory = jarFolder;
            startInfo.Arguments = $"\"{batPath}\"";
        
            // This forces Windows to execute it natively via the explorer shell
            startInfo.UseShellExecute = true; 
            startInfo.CreateNoWindow = false; // Set to true later to hide the window

            Process.Start(startInfo);
            Debug.Log("Batch file executed successfully!");
            StartCoroutine(MonitorSasProgram());
        }   
        else if (FileType == "exe")
        {
            var myProcess = new Process();
            myProcess.StartInfo.UseShellExecute = false;
            myProcess.StartInfo.WorkingDirectory = rootDir;
            string fullExePath = Path.Combine(rootDir, path, execName);
            myProcess.StartInfo.FileName = fullExePath;
            myProcess.Start();
        }
    }
    
    //Watch for when the game is closed then alow the controler to reconect to the unity project
    IEnumerator MonitorSasProgram()
    {
        // Give the batch file and game a moment to initialize
        yield return new WaitForSeconds(2.0f);

        while (_controler.gameIsRunning)
        {
            yield return new WaitForSeconds(.1f);

            // Look for the exact process name as it appears in Task Manager (minus the .exe)
            Process[] processes = Process.GetProcesses();
            bool foundMatch = false;

            foreach (Process p in processes)
            {
                if (!string.IsNullOrEmpty(p.MainWindowTitle))
                {
                    // Check if the title matches the first name OR the second name
                    bool matchesFirst = p.MainWindowTitle.IndexOf("sas-program", System.StringComparison.OrdinalIgnoreCase) >= 0;
                    bool matchesSecond = p.MainWindowTitle.IndexOf("SMIMS Frogger", System.StringComparison.OrdinalIgnoreCase) >= 0;

                    if (matchesFirst || matchesSecond)
                    {
                        foundMatch = true;
                        break; // Found it, no need to keep looking through this frame
                    }
                }
            }
            if (!foundMatch)
            {
                _controler.gameIsRunning = false;
            }
        }

        Debug.Log("SAS Program closed. Reclaiming the Arduino port.");
        _controler.gameIsRunning = false;
    }
}