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
            var processStartInfo = new ProcessStartInfo();
            processStartInfo.UseShellExecute = true;
            processStartInfo.CreateNoWindow = false;
            processStartInfo.WorkingDirectory = rootDir ;
            processStartInfo.FileName = "cmd.exe";
            string fullJarPath = $"{rootDir}{path}{execName}";
            processStartInfo.Arguments = $"/k java -jar \"{fullJarPath}\"";
            try
            {
                Process.Start(processStartInfo);
                Debug.Log("Jar Launched");
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to Launch jar"+ e.Message);
            }
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
}