using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.UI;
using System.IO;

public class WebCamMic : MonoBehaviour
{
    int currentCamIndex = 0; // 0 - built in camera, 1 - usb camera

    WebCamTexture tex;

    public RawImage display;
    public int[] snapSizes = new int[3]; // width, height, tex.width * tex.height

    public Text stopStartText;

    public Text stopStartMicText;
    public Text cameraText;
    public Text microphoneText;
    public Text takeAudioText;
    public Text switchMicrophoneText;
    public Text filenameText;

    private string filename;
    private string reportFilename;

    private string fileNamePrefix = "meandr_";
    string path; // = Application.dataPath; //="C:/Users/kpervushin/Downloads"; //="/storage/emulated/0/Download/";
    
#region MICROPHONE
    private AudioSource audioSource;
    private string _deviceMic;
    private int currentMicIndex = 0; // 0 - built in camera, 1 - usb camera

    AudioClip _clipRecord;

    private float heightMultiplier = -1.0f;
    private static int numberOfSamples = 1024;
    public FFTWindow fftWindow;
    private float[] spectrum = new float[numberOfSamples];
    public GameObject[] circles;

    private Vector3 origin = new Vector3(0f, -3.0f, 0f);
    private float radius =  0.3f; 
    private float speed = 0.25f;
    private float rotationDirection =0f;
    private float pos = 0f;

    private int micStatus = 0; 
    // 0, do nothing, 1, mic activated for checking
    // 2, clip recording started
    // 3, clip recording ended
    // 4, clip playback started
    // 5, clip playback ended, reurn to 0

// Audio Mixer controls
    [SerializeField] string _volumeParameter = "MasterVolume"; 
    [SerializeField] AudioMixer _mixer; // set to -MAsterVolume at -80 at .play to remove echo, set to 0 at playback

#endregion

#region PSY
    public float excitedDepressed = 0f; // -10, +10
    public float exhausedEnergized = 0f; // -10, +10
    public float stressedRelaxed = 0f; // -10, +10
#endregion
  
    private void StopMicrophone()
    {
        if(audioSource != null){
            audioSource.Stop();
            audioSource = null;
            stopStartMicText.text = "Start Mic";
        }
        
    }

    public void StartStopMic_Clicked(){

        if((micStatus==3)||(micStatus==5)){ // Recoding finished, or PLay finished

            micStatus=6;
            stopStartMicText.text = "Start/Stop Mic";
            switchMicrophoneText.text = "Switch Microphone";
            

            reportFilename = fileNamePrefix+ System.DateTime.Now.ToString("yy-MM-dd-hh-mm-tt");
            filename =path +"/tmp/"+reportFilename; 
            filenameText.text = reportFilename;

            SavWav.Save(filename, audioSource.clip);
            filenameText.text = "Audio taken: "+reportFilename+".wav";
            takeAudioText.text = "Take Audio";
            takeAudioText.color = Color.yellow;

            string outS = "Stressed-Relaxed, " + stressedRelaxed.ToString("##.#")+",   ";
            outS += "Exhaused-Energized, " + exhausedEnergized.ToString("##.#")+",   ";
            outS += "Depressed-Excited, " + excitedDepressed.ToString("##.#")+",   ";
            SavWav.WriteString(filename,outS);

            StopMicrophone();
            ResetAudioButtons();


        }

        if(micStatus==0){  
            // micStatus=1;
            if(audioSource != null)
            {
                StopMicrophone();

            }
            else
            {
                audioSource = GetComponent<AudioSource>();
                _deviceMic = Microphone.devices[currentMicIndex];
// Debug.Log("Mic Device name: " + _deviceMic.ToString());
                microphoneText.text = _deviceMic.ToString();
                audioSource.clip = Microphone.Start(_deviceMic, true, 999, 44100); // loop and 999 sec rec
        // Mute the sound with an Audio Mixer group bc we don't want the player to hear it
// https://www.youtube.com/watch?v=CdNBsWowRbE  7:32 (isRecording check )
                if(Microphone.IsRecording(_deviceMic)){
                    while(!(Microphone.GetPosition(_deviceMic)>0)){} //Wait untill recording is started
                } else {
            // microphone does nt work for some reason
                    Debug.Log(_deviceMic.ToString() + " does not work!");
                    microphoneText.text = _deviceMic.ToString()+ " doesn't not Start!";
                }


            float volumeOut = -80f;
            _mixer.SetFloat(_volumeParameter, volumeOut);

            audioSource.Play();

            stopStartMicText.text = "Stop Mic";
        }
        }//if(micStatus==0){
    }




    public void SwapMic_Clicked(){

        if(micStatus==3){
            ResetAudioButtons();
        } else {


            if( Microphone.devices.Length > 0 )
                {
                    currentMicIndex +=1;
                    currentMicIndex %= Microphone.devices.Length;  //  Inx not to go beyound Length

// Debug.Log("currentMicIndex = " + currentMicIndex);

            if(audioSource != null)
                {
                    StopMicrophone();
                    StartStopMic_Clicked();
                }

        }
        PlayerPrefs.SetInt("currentMicIndex",currentMicIndex); 
        }

    }

    public void SwapCam_Clicked(){

//        Debug.Log("SwapCam -- button pressed");

        if( WebCamTexture.devices.Length > 0 )
        {
        currentCamIndex +=1;
        currentCamIndex %= WebCamTexture.devices.Length;  //  Inx not to go beyound Length

// Debug.Log("currentCamIndex = " + currentCamIndex);

        if(tex != null)
        {
            StopCamera();
            StartStopCam_Clicked();
        }

        }

// Debug.Log("onDisable: currentCamIndex: "+currentCamIndex);
        PlayerPrefs.SetInt("currentCamIndex",currentCamIndex);
    }

    public void StartStopCam_Clicked(){

        //Debug.Log("Start -- button pressed");
        if(tex != null)
        {
            StopCamera();

        }
        else
        {
        WebCamDevice device = WebCamTexture.devices[currentCamIndex];
        Debug.Log("Device name: " + device.name);
        //tictocText.text = device.name; // output as text above video
        cameraText.text = device.name;
        tex = new WebCamTexture(device.name);

        // tex.requestedFPS=60;
        // tex.requestedWidth=1024;
        // tex.requestedHeight=576;

        display.texture = tex;
        // tex.requestedFPS=60;
        // tex.requestedWidth=864; // 5 ms, SOLUTION!
        // tex.requestedHeight=480;
        tex.Play();
        stopStartText.text = "Stop Cam";
        }
    }

    private void StopCamera()
    {
        display.texture = null;
        tex.Stop();
        tex = null;
        stopStartText.text = "Start Cam";
    }

    public void Snap_Clicked(){

        //tic = Time.realtimeSinceStartup;
        // https://docs.unity3d.com/ScriptReference/Time-realtimeSinceStartup.html
        //Debug.Log("In Snap_Clicked, Time.realtimeSinceStartup: " + tic);

        if(tex != null) StartCoroutine(TakePhoto()); 

        //toc = Time.realtimeSinceStartup;
        //tictoc = toc - tic;
        //Debug.Log("In Snap_Clicked, tictoc: " + tictoc);

        //string s = string.Format("Snap time {0} s", tictoc);
        //tictocText.text = s.ToString();

    }

    public void TakeAudio_Clicked(){
        StartCoroutine(TakeAudio()); 
    }
    
    IEnumerator TakePhoto()  // Start this Coroutine on some button click
    {

    // NOTE - you almost certainly have to do this here:
    //Debug.Log("in TakePhoto: yield return");
     yield return new WaitForEndOfFrame(); 

    // it's a rare case where the Unity doco is pretty clear,
    // http://docs.unity3d.com/ScriptReference/WaitForEndOfFrame.html
    // be sure to scroll down to the SECOND long example on that doco page 

        Texture2D photo = new Texture2D(tex.width, tex.height);
        photo.SetPixels(tex.GetPixels());
        photo.Apply();

       
        // var pixels = new Color[tex.width * tex.height];

        // pixels = tex.GetPixels();

        reportFilename = fileNamePrefix+ System.DateTime.Now.ToString("yy-MM-dd-hh-mm-tt");
        filename =path +"/tmp/"+reportFilename; 
        filenameText.text = reportFilename;
// Debug.Log("filename: " +filename);
        byte[] bytes = photo.EncodeToPNG();
        File.WriteAllBytes(filename+".png", bytes);
        filenameText.text = "Photo taken: "+reportFilename+".png";
    }


    IEnumerator TakeAudio(){

            if(micStatus ==4 ){ // record ready to play
            takeAudioText.text = "Take Audio";
            takeAudioText.color = Color.blue;
            micStatus=5; //  clip is ready to save or discard
            audioSource.Stop(); 
            yield return new WaitForSeconds(1); // 3 more seconds of recording
            stopStartMicText.text = "Save Rec";


        } 

         if(micStatus ==3 ){ // record ready to play
            _mixer.SetFloat(_volumeParameter, 0f);
            takeAudioText.text = "Stop Play";
            takeAudioText.color = Color.yellow;
            micStatus=4; //  playing is going, ready to stop

            yield return new WaitForSeconds(1); // 3 more seconds of recording
// Debug.Log("Playing clip");
            audioSource.Play(); 
        } 

        if(micStatus ==2 ){ // recording is in progress
            takeAudioText.text = "3 more sec..";
            takeAudioText.color = Color.yellow;
            micStatus=3; //  recording stopped, ready to play

            yield return new WaitForSeconds(3); // 3 more seconds of recording
            takeAudioText.text = "Play Rec";
            if(Microphone.IsRecording(_deviceMic) == true) {Microphone.End(_deviceMic);}
            audioSource.Stop(); 
            stopStartMicText.text = "Save Rec";
            switchMicrophoneText.text = "Reset";


        } 
        
        if(micStatus ==0 ){ // 0, idling, ready to start rec
        // if(audioSource != null)
        // {
            StopMicrophone();
            audioSource = GetComponent<AudioSource>();
            _deviceMic = Microphone.devices[currentMicIndex];
// Debug.Log("Take Audio: Mic Device name: " + _deviceMic.ToString());
            microphoneText.text = _deviceMic.ToString();
            audioSource.clip = Microphone.Start(_deviceMic, false, 30, 44100); // no-loop and 30 sec rec
            if(Microphone.IsRecording(_deviceMic)){
                while(!(Microphone.GetPosition(_deviceMic)>0)){} //Wait untill recording is started
            } else {
            // microphone does nt work for some reason
                Debug.Log(_deviceMic.ToString() + " does not work!");
                microphoneText.text = _deviceMic.ToString()+ " doesn't not Start!";
            }


            float volumeOut = -80f;
            _mixer.SetFloat(_volumeParameter, volumeOut);

            audioSource.Play();
            if(Microphone.IsRecording(_deviceMic) == true){
                takeAudioText.text = "Stop Rec";
                takeAudioText.color = Color.green;
                micStatus=2; // Start recording
                yield return new WaitForSeconds(1);
            }

        }//if(micStatus ==2 ){

        // reportFilename = fileNamePrefix+ System.DateTime.Now.ToString("yy-MM-dd-hh-mm-tt");
        // filename =path +"/tmp/"+reportFilename; 
        // filenameText.text = reportFilename;

        // SavWav.Save(filename, audioSource.clip);
        // filenameText.text = "Audio taken: "+reportFilename+".wav";
        // takeAudioText.text = "Take Audio";
        // takeAudioText.color = Color.yellow;

        // string outS = "Stressed-Relaxed, " + stressedRelaxed.ToString("##.#")+",   ";
        // outS += "Exhaused-Energized, " + exhausedEnergized.ToString("##.#")+",   ";
        // outS += "Depressed-Excited, " + excitedDepressed.ToString("##.#")+",   ";
        // SavWav.WriteString(filename,outS);



//         // audioSource.Play();



//         stopStartMicText.text = "Stop Mic";

       // }//if(audio)

    }//TakeAudio


private void ResetAudioButtons(){
    
    StopMicrophone();
    takeAudioText.text = "Take Audio";
    takeAudioText.color = Color.green;
    stopStartMicText.text = "Start/Stop Mic";
    switchMicrophoneText.text = "Switch Microphone";

}



    public void ReturnHome(){
        SceneManager.LoadScene("MainMenu");
    }

public void ExcitedDepressed(float newExcitedDepressed){
    excitedDepressed = newExcitedDepressed;
    // Debug.Log("excitedDepressed: "+ excitedDepressed);
}
public void ExhausedEnergized(float newExhausedEnergized){
    exhausedEnergized = newExhausedEnergized;
    // Debug.Log("exhausedEnergized: "+ exhausedEnergized);
}
public void StressedRelaxed(float newStressedRelaxed){
    stressedRelaxed = newStressedRelaxed;
    // Debug.Log("stressedRelaxed: "+ stressedRelaxed);
}



private void onDisable(){  // is not invoked in unity editor

// Debug.Log("onDisable: currentCamIndex: "+currentCamIndex);
//         PlayerPrefs.SetInt("currentCamIndex",currentCamIndex);
//         PlayerPrefs.SetInt("currentMicIndex",currentMicIndex); 

//         reportFilename = fileNamePrefix+ System.DateTime.Now.ToString("yy-MM-dd-hh-mm-tt");
//         filename =path +"/tmp/"+reportFilename; 
//         filenameText.text = reportFilename;
// // Debug.Log("filename: " +filename);
//         string outS = "Stressed-Relaxed, " + stressedRelaxed.ToString("##.#")+",   ";
//         outS += "Exhaused-Energized, " + exhausedEnergized.ToString("##.#")+",   ";
//         outS += "Depressed-Excited, " + excitedDepressed.ToString("##.#")+",   ";
//         SavWav.WriteString(filename,outS);


}
void Start(){
    // filename for saving png and wav
    // path = Application.dataPath+"/";
    path = Application.persistentDataPath+"/";  
Debug.Log("Application.persistentDataPath: "+path);
filenameText.text = path;
    //="/storage/emulated/0/Download/";

    currentCamIndex = PlayerPrefs.GetInt("currentCamIndex",currentCamIndex);
    currentMicIndex = PlayerPrefs.GetInt("currentMicIndex",currentMicIndex);
// Debug.Log("Start: currentCamIndex: "+currentCamIndex);

}

 public void Update(){

     GetComponent<AudioSource>().GetSpectrumData(spectrum,0,fftWindow);
// Debug.Log("Spectrum 200 : " + spectrum[200]);

// Position and rotate circles

    for(int i=0;i<8;i++){
        float freqsum =0f;
        for(int j=(i*numberOfSamples/8); j< ((i+1)*numberOfSamples/8);j++) {
            freqsum +=spectrum[j];
        }
        if(freqsum < 1e-11f){freqsum = 1e-11f;}
// Debug.Log("log freqsum: " + i + " sum: " + Mathf.Log(freqsum, 3));


    if((micStatus ==2) && (Microphone.IsRecording(_deviceMic) == false) ){
	    audioSource.Stop(); 
	    micStatus=3;
        takeAudioText.text = "Play Rec";
        takeAudioText.color = Color.green;
    }

    if(micStatus==1) {
        rotationDirection = 1f;
    }
    if(micStatus==0) {
        rotationDirection = 0.2f;
    }
    if(micStatus==3) {
        rotationDirection = -0.2f;
    }
    if(micStatus==2) {
        rotationDirection = -1f;
    }
    if(micStatus==4) {
        rotationDirection = 0.2f;
    }
        if(micStatus==6) {
        micStatus=0;
    }
        pos += rotationDirection*speed*Time.deltaTime;
        float x = Mathf.Sin(pos+2f*3.14159f/8f*i)*(radius + heightMultiplier/Mathf.Log(freqsum, 3));
        float y = Mathf.Cos(pos+2f*3.14159f/8f*i)*(radius + heightMultiplier/Mathf.Log(freqsum, 3));
        circles[i].transform.position = new Vector3(x,y,0f) + origin;
    }
// cameraText.text = "micStatus: " + micStatus.ToString();

 }//Update



}//class
