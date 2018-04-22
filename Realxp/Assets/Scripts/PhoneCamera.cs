using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class PhoneCamera : MonoBehaviour {

    private bool camAvailable;
    private WebCamTexture webcamTexture;
    private Texture defaultBackground;

    public RawImage background;
    public AspectRatioFitter fit;

    public string url = "https://vision.googleapis.com/v1/images:annotate?key=";
    public string apiKey = "";

    public FeatureType featureType = FeatureType.LABEL_DETECTION;
    public int maxResults = 100;

    Texture2D texture2D;
    Dictionary<string, string> headers;

    public List<string> randomPoolList;
    public List<string> restrictedList;

    public List<string> currentWordsList;

    public string currentWord;

    public float score;
    public int deletes = 10;

    public Text levelText;
    public Text scoreText;
    public List<Text> currentWordTexts;
    public Text deletesText;

    long lockTime = 0;
    private int maxDeletes = 99;
    private System.TimeSpan waitTime = System.TimeSpan.FromMinutes(1);

    public string result;

    public Canvas[] canvases;
    public UnityEngine.UI.Image expBar;

    [System.Serializable]
    public class AnnotateImageRequests
    {
        public List<AnnotateImageRequest> requests;
    }

    [System.Serializable]
    public class AnnotateImageRequest
    {
        public Image image;
        public List<Feature> features;
    }

    [System.Serializable]
    public class Image
    {
        public string content;
    }

    [System.Serializable]
    public class Feature
    {
        public string type;
        public int maxResults;
    }

    [System.Serializable]
    public class ImageContext
    {
        public LatLongRect latLongRect;
        public List<string> languageHints;
    }

    [System.Serializable]
    public class LatLongRect
    {
        public LatLng minLatLng;
        public LatLng maxLatLng;
    }

    [System.Serializable]
    public class AnnotateImageResponses
    {
        public List<AnnotateImageResponse> responses;
    }

    [System.Serializable]
    public class AnnotateImageResponse
    {
        public List<FaceAnnotation> faceAnnotations;
        public List<EntityAnnotation> landmarkAnnotations;
        public List<EntityAnnotation> logoAnnotations;
        public List<EntityAnnotation> labelAnnotations;
        public List<EntityAnnotation> textAnnotations;
    }

    [System.Serializable]
    public class FaceAnnotation
    {
        public BoundingPoly boundingPoly;
        public BoundingPoly fdBoundingPoly;
        public List<Landmark> landmarks;
        public float rollAngle;
        public float panAngle;
        public float tiltAngle;
        public float detectionConfidence;
        public float landmarkingConfidence;
        public string joyLikelihood;
        public string sorrowLikelihood;
        public string angerLikelihood;
        public string surpriseLikelihood;
        public string underExposedLikelihood;
        public string blurredLikelihood;
        public string headwearLikelihood;
    }

    [System.Serializable]
    public class EntityAnnotation
    {
        public string mid;
        public string locale;
        public string description;
        public float score;
        public float confidence;
        public float topicality;
        public BoundingPoly boundingPoly;
        public List<LocationInfo> locations;
        public List<Property> properties;
    }

    [System.Serializable]
    public class BoundingPoly
    {
        public List<Vertex> vertices;
    }

    [System.Serializable]
    public class Landmark
    {
        public string type;
        public Position position;
    }

    [System.Serializable]
    public class Position
    {
        public float x;
        public float y;
        public float z;
    }

    [System.Serializable]
    public class Vertex
    {
        public float x;
        public float y;
    }

    [System.Serializable]
    public class LocationInfo
    {
        LatLng latLng;
    }

    [System.Serializable]
    public class LatLng
    {
        float latitude;
        float longitude;
    }

    [System.Serializable]
    public class Property
    {
        string name;
        string value;
    }

    public enum FeatureType
    {
        TYPE_UNSPECIFIED,
        FACE_DETECTION,
        LANDMARK_DETECTION,
        LOGO_DETECTION,
        LABEL_DETECTION,
        TEXT_DETECTION,
        SAFE_SEARCH_DETECTION,
        IMAGE_PROPERTIES
    }

    public enum LandmarkType
    {
        UNKNOWN_LANDMARK,
        LEFT_EYE,
        RIGHT_EYE,
        LEFT_OF_LEFT_EYEBROW,
        RIGHT_OF_LEFT_EYEBROW,
        LEFT_OF_RIGHT_EYEBROW,
        RIGHT_OF_RIGHT_EYEBROW,
        MIDPOINT_BETWEEN_EYES,
        NOSE_TIP,
        UPPER_LIP,
        LOWER_LIP,
        MOUTH_LEFT,
        MOUTH_RIGHT,
        MOUTH_CENTER,
        NOSE_BOTTOM_RIGHT,
        NOSE_BOTTOM_LEFT,
        NOSE_BOTTOM_CENTER,
        LEFT_EYE_TOP_BOUNDARY,
        LEFT_EYE_RIGHT_CORNER,
        LEFT_EYE_BOTTOM_BOUNDARY,
        LEFT_EYE_LEFT_CORNER,
        RIGHT_EYE_TOP_BOUNDARY,
        RIGHT_EYE_RIGHT_CORNER,
        RIGHT_EYE_BOTTOM_BOUNDARY,
        RIGHT_EYE_LEFT_CORNER,
        LEFT_EYEBROW_UPPER_MIDPOINT,
        RIGHT_EYEBROW_UPPER_MIDPOINT,
        LEFT_EAR_TRAGION,
        RIGHT_EAR_TRAGION,
        LEFT_EYE_PUPIL,
        RIGHT_EYE_PUPIL,
        FOREHEAD_GLABELLA,
        CHIN_GNATHION,
        CHIN_LEFT_GONION,
        CHIN_RIGHT_GONION
    };

    public enum Likelihood
    {
        UNKNOWN,
        VERY_UNLIKELY,
        UNLIKELY,
        POSSIBLE,
        LIKELY,
        VERY_LIKELY
    }

    private void Start () {
        //PlayerPrefs.DeleteAll();
        //PlayerPrefs.SetInt("Deletes", 10);
        lockTime = System.DateTime.Now.Ticks;

        //randomPoolList = new List<string> { "leg", "tree", "flower", "arm", "furniture", "finger", "nail", "chin", "nose", "head", "eye", "girl", "room", "glasses", "ceiling", "floor", "hand", "shoe", "sock", "water" };
        //File.WriteAllLines(Path.Combine(Application.persistentDataPath, "randomPool.txt"), randomPoolList.ToArray());
        if (!File.Exists(Path.Combine(Application.persistentDataPath, "randomPool.txt")))
        {
            randomPoolList = new List<string> { "leg", "tree", "flower", "arm", "furniture", "finger", "nail", "chin", "nose", "head", "eye", "girl", "room", "glasses", "ceiling", "floor", "hand", "shoe", "sock", "water" };
            File.WriteAllLines(Path.Combine(Application.persistentDataPath, "randomPool.txt"), randomPoolList.ToArray());
        }
        else
        {
            randomPoolList = new List<string>(File.ReadAllLines(Path.Combine(Application.persistentDataPath, "randomPool.txt")));
        }


        //FillCurrentWords();
        //File.WriteAllLines(Path.Combine(Application.persistentDataPath, "currentWords.txt"), currentWordsList.ToArray());
        if (!File.Exists(Path.Combine(Application.persistentDataPath, "currentWords.txt")))
        {
            FillCurrentWords();
            File.WriteAllLines(Path.Combine(Application.persistentDataPath, "currentWords.txt"), currentWordsList.ToArray());
        }
        else
        {
            currentWordsList = new List<string>(File.ReadAllLines(Path.Combine(Application.persistentDataPath, "currentWords.txt")));
        }

        UpdateTextFields();

        headers = new Dictionary<string, string>();
        headers.Add("Content-Type", "application/json; charset=UTF-8");
        if (apiKey == null || apiKey == "")
            Debug.LogError("No API key. Please set your API key into the \"Web Cam Texture To Cloud Vision(Script)\" component.");

        defaultBackground = background.texture;
        WebCamDevice[] devices = WebCamTexture.devices;
        
        if(devices.Length == 0)
        {
            Debug.Log("No Camera detected");
            camAvailable = false;
            return;
        }

        //for(int i = 0; i < devices.Length; i++)
        //{
        //    if (!devices[i].isFrontFacing)
        //    {
        //        webcamTexture = new WebCamTexture(devices[i].name, Screen.width, Screen.height);
        //    }
        //}
        webcamTexture = new WebCamTexture(devices[0].name, Screen.width, Screen.height);

        if (webcamTexture == null)
        {
            Debug.Log("Unable to find back camera");
            return;
        }

        webcamTexture.Play();
        background.texture = webcamTexture;

        camAvailable = true;
	}

    public void FillCurrentWords()
    {
        for(int i = 0; i < 9; i++)
        {
            string wordToAdd = randomPoolList[Random.Range(0, randomPoolList.Count)];
            while(currentWordsList.Contains(wordToAdd))
            {
                wordToAdd = randomPoolList[Random.Range(0, randomPoolList.Count)];
            }
            currentWordsList.Add(wordToAdd);
        }
    }

    public void ReplaceWord(int index)
    {
        string wordToAdd = randomPoolList[Random.Range(0, randomPoolList.Count)];
        while (currentWordsList.Contains(wordToAdd))
        {
            wordToAdd = randomPoolList[Random.Range(0, randomPoolList.Count)];
        }
        currentWordsList[index] = wordToAdd;

        File.WriteAllLines(Path.Combine(Application.persistentDataPath, "currentWords.txt"), currentWordsList.ToArray());
    }

    public void RefreshWord(int index)
    {
        if(PlayerPrefs.GetInt("Deletes") > 0)
        {
            lockTime = System.DateTime.Now.Ticks;
            ReplaceWord(index);
            PlayerPrefs.SetInt("Deletes", PlayerPrefs.GetInt("Deletes") - 1);
            UpdateTextFields();
        }
        

    }

	void Update ()
    {
        if(lockTime > 0)
        {
            while(System.DateTime.Now.Ticks >= lockTime + waitTime.Ticks && PlayerPrefs.GetInt("Deletes") < maxDeletes)
            {
                PlayerPrefs.SetInt("Deletes", PlayerPrefs.GetInt("Deletes") + 1);
                lockTime += waitTime.Ticks;
                UpdateTextFields();
            }
        }

	    if(!camAvailable)
        {
            return;
        }
        float ratio = (float)webcamTexture.width / (float)webcamTexture.height;
        fit.aspectRatio = ratio;

        float scaleY = webcamTexture.videoVerticallyMirrored ? -1f: 1f;
        background.rectTransform.localScale = new Vector3(1f, scaleY, 1f);

        int orient = -webcamTexture.videoRotationAngle;
        background.rectTransform.localEulerAngles = new Vector3(0, 0, orient);
	}

    private IEnumerator SingleCapture()
    {
        if (this.apiKey == null)
            yield return null;

        Color[] pixels = webcamTexture.GetPixels();
        if (pixels.Length == 0)
            yield return null;
        if (texture2D == null || webcamTexture.width != texture2D.width || webcamTexture.height != texture2D.height)
        {
            texture2D = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGBA32, false);
        }

        texture2D.SetPixels(pixels);
        // texture2D.Apply(false); // Not required. Because we do not need to be uploaded it to GPU
        byte[] jpg = texture2D.EncodeToJPG();
        string base64 = System.Convert.ToBase64String(jpg);
#if UNITY_WEBGL
			Application.ExternalCall("post", this.gameObject.name, "OnSuccessFromBrowser", "OnErrorFromBrowser", this.url + this.apiKey, base64, this.featureType.ToString(), this.maxResults);
#else

        AnnotateImageRequests requests = new AnnotateImageRequests();
        requests.requests = new List<AnnotateImageRequest>();

        AnnotateImageRequest request = new AnnotateImageRequest();
        request.image = new Image();
        request.image.content = base64;
        request.features = new List<Feature>();

        Feature feature = new Feature();
        feature.type = this.featureType.ToString();
        feature.maxResults = this.maxResults;

        request.features.Add(feature);

        requests.requests.Add(request);

        string jsonData = JsonUtility.ToJson(requests, false);
        if (jsonData != string.Empty)
        {
            string url = this.url + this.apiKey;
            byte[] postData = System.Text.Encoding.Default.GetBytes(jsonData);
            using (WWW www = new WWW(url, postData, headers))
            {
                yield return www;
                if (string.IsNullOrEmpty(www.error))
                {
                    //Debug.Log(www.text.Replace("\n", "").Replace(" ", ""));
                    AnnotateImageResponses responses = JsonUtility.FromJson<AnnotateImageResponses>(www.text);
                    // SendMessage, BroadcastMessage or someting like that.
                    Sample_OnAnnotateImageResponses(responses);
                }
                else
                {
                    Debug.Log("Error: " + www.error);
                }
            }
        }
#endif
    }

    public void StartCapture()
    {
        StartCoroutine("SingleCapture");
    }

    void Sample_OnAnnotateImageResponses(AnnotateImageResponses responses)
    {
        int howManyRight = 0;
        float tempScore = 0;

        bool foundWord = false;

        bool[] toReplace = new bool[] { false, false, false, false, false, false, false, false, false };

        if (responses.responses.Count > 0)
        {
            if (responses.responses[0].labelAnnotations != null && responses.responses[0].labelAnnotations.Count > 0)
            {
                for(int i = 0; i < responses.responses[0].labelAnnotations.Count; i++)
                {
                    Debug.Log(responses.responses[0].labelAnnotations[i].description);
                    Debug.Log(responses.responses[0].labelAnnotations[i].score);

                    for(int j = 0; j < currentWordsList.Count; j++)
                    {
                        if (currentWordsList[j] == responses.responses[0].labelAnnotations[i].description)
                        {
                            howManyRight++;
                            tempScore += responses.responses[0].labelAnnotations[i].score;
                            toReplace[j] = true;
                            foundWord = true;
                        }
                    }       
                }
            }
        }

        if(foundWord)
        {
            foreach(var response in responses.responses[0].labelAnnotations)
            {
                if(!randomPoolList.Contains(response.description) && response.score > 0.75 && response.description.Split(' ').Length - 1 <= 1)
                {
                    randomPoolList.Add(response.description);
                }
            }

            File.WriteAllLines(Path.Combine(Application.persistentDataPath, "currentWords.txt"), randomPoolList.ToArray());
        }

        PlayerPrefs.SetFloat("Experience", PlayerPrefs.GetFloat("Experience") + howManyRight * tempScore * 100);

        for(int i = 0; i < toReplace.Length; i++)
        {
            if(toReplace[i])
            {
                ReplaceWord(i);
            }
        }
        UpdateTextFields();
    }

    private void UpdateTextFields()
    {
        float temp = PlayerPrefs.GetFloat("Experience");
        int count = 0;
        for (count = 0; temp - count * 100 > 0; count++)
        {
            temp -= count * 100;
        }
        if(count <= 0)
        {
            count = 1;
        }
        levelText.text = "Lvl: " + (count - 1).ToString();
        scoreText.text = (int)temp + " / " + count * 100;

        expBar.fillAmount = (temp / (count * 100));
        for (int i = 0; i < currentWordTexts.Count; i++)
        {
            currentWordTexts[i].text = currentWordsList[i];
        }
        deletesText.text = PlayerPrefs.GetInt("Deletes").ToString();
    }

    public void ChangeScene(int index)
    {
        for (int i = 0; i < canvases.Length; i++)
        {
            if(i == index)
            {
                canvases[i].gameObject.SetActive(true);
            }
            else
            {
                canvases[i].gameObject.SetActive(false);
            }
        }
    }

    public void ShowWords()
    {
        foreach(Transform child in canvases[1].transform)
        {
            if(child.name != "Word Square Background")
            {
                child.gameObject.SetActive(false);
            }
            else
            {
                UnityEngine.UI.Image[] images = child.GetComponentsInChildren<UnityEngine.UI.Image>();
                foreach (var image in images)
                {
                    image.color = new Color(1, 1, 1, 0.5f);
                }
            }
            canvases[1].gameObject.SetActive(true);
        }
    }

    public void HideWords()
    {
        canvases[1].gameObject.SetActive(false);
        foreach (Transform child in canvases[1].transform)
        {
            if(child.name == "World Square Background")
            {
                UnityEngine.UI.Image[] images = child.GetComponentsInChildren<UnityEngine.UI.Image>();
                foreach (var image in images)
                {
                    image.color = new Color(1, 1, 1, 1);
                }
            }
            child.gameObject.SetActive(true);
        }
    }
}
