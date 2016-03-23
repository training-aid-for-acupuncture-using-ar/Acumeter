using UnityEngine;
using System.Collections;
using UnityEngine.UI;

using OpenCVForUnity;
using System.Collections.Generic;

namespace OpenCVForUnitySample
{

    public class HandPoseEstimationSample : MonoBehaviour
    {

        /// <summary>
        /// The web cam texture.
        /// </summary>
        WebCamTexture webCamTexture;

        /// <summary>
        /// The web cam device.
        /// </summary>
        WebCamDevice webCamDevice;

        /// <summary>
        /// The colors.
        /// </summary>
        Color32[] colors;

        /// <summary>
        /// Should use front facing.
        /// </summary>
        public bool shouldUseFrontFacing = false;

        /// <summary>
        /// The width.
        /// </summary>
        // int width = 640;
        int width = 1280;
        /// <summary>
        /// The height.
        /// </summary>
        // int height = 480;
        int height = 960;
        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The init done.
        /// </summary>
        bool initDone = false;

        /// <summary>
        /// The screenOrientation.
        /// </summary>
        ScreenOrientation screenOrientation = ScreenOrientation.Unknown;

        /// <summary>
        /// The rgba mat.
        /// </summary>
        private Mat rgbaMat;

        /// <summary>
        /// The threashold slider.
        /// </summary>
        public Slider threasholdSlider;

        /// <summary>
        /// The BLOB color hsv.
        /// </summary>
        private Scalar blobColorHsv;

        //				/// <summary>
        //				/// The BLOB color rgba.
        //				/// </summary>
        //				private Scalar blobColorRgba;

        /// <summary>
        /// The detector.
        /// </summary>
        private ColorBlobDetector detector;

        /// <summary>
        /// The spectrum mat.
        /// </summary>
        private Mat spectrumMat;

        /// <summary>
        /// The is color selected.
        /// </summary>
        private bool isColorSelected = false;

        /// <summary>
        /// The SPECTRU m_ SIZ.
        /// </summary>
        private Size SPECTRUM_SIZE;

        /// <summary>
        /// The CONTOU r_ COLO.
        /// </summary>
        private Scalar CONTOUR_COLOR;

        /// <summary>
        /// The CONTOU r_ COLO r_ WHIT.
        /// </summary>
        private Scalar CONTOUR_COLOR_WHITE;

        /// <summary>
        /// The number of fingers.
        /// </summary>
        int numberOfFingers = 0;



        public static int handmode = 0; //0,1,2,3    0,1왼쪽 2,3 오른쪽  0손안쪽, 1손바깥

        /// <summary>
        /// The number of fingers text.
        /// </summary>
        public Text numberOfFingersText;


        // Use this for initialization
        void Start()
        {

            StartCoroutine(init());

        }

        private IEnumerator init()
        {
            if (webCamTexture != null)
            {
                webCamTexture.Stop();
                initDone = false;

                rgbaMat.Dispose();
                spectrumMat.Dispose();
            }

            // Checks how many and which cameras are available on the device
            for (int cameraIndex = 0; cameraIndex < WebCamTexture.devices.Length; cameraIndex++)
            {


                if (WebCamTexture.devices[cameraIndex].isFrontFacing == shouldUseFrontFacing)
                {


                    Debug.Log(cameraIndex + " name " + WebCamTexture.devices[cameraIndex].name + " isFrontFacing " + WebCamTexture.devices[cameraIndex].isFrontFacing);

                    webCamDevice = WebCamTexture.devices[cameraIndex];

                    webCamTexture = new WebCamTexture(webCamDevice.name, width, height);

                    break;
                }


            }

            if (webCamTexture == null)
            {
                webCamDevice = WebCamTexture.devices[0];
                webCamTexture = new WebCamTexture(webCamDevice.name, width, height);
            }

            Debug.Log("width " + webCamTexture.width + " height " + webCamTexture.height + " fps " + webCamTexture.requestedFPS);



            // Starts the camera
            webCamTexture.Play();


            while (true)
            {
                //If you want to use webcamTexture.width and webcamTexture.height on iOS, you have to wait until webcamTexture.didUpdateThisFrame == 1, otherwise these two values will be equal to 16. (http://forum.unity3d.com/threads/webcamtexture-and-error-0x0502.123922/)
#if UNITY_IOS && !UNITY_EDITOR && (UNITY_4_6_3 || UNITY_4_6_4 || UNITY_5_0_0 || UNITY_5_0_1)
				                if (webCamTexture.width > 16 && webCamTexture.height > 16) {
#else
                if (webCamTexture.didUpdateThisFrame)
                {
#if UNITY_IOS && !UNITY_EDITOR && UNITY_5_2
										while (webCamTexture.width <= 16) {
												webCamTexture.GetPixels32 ();
												yield return new WaitForEndOfFrame ();
										} 
#endif
#endif

                    Debug.Log("width " + webCamTexture.width + " height " + webCamTexture.height + " fps " + webCamTexture.requestedFPS);
                    Debug.Log("videoRotationAngle " + webCamTexture.videoRotationAngle + " videoVerticallyMirrored " + webCamTexture.videoVerticallyMirrored + " isFrongFacing " + webCamDevice.isFrontFacing);

                    colors = new Color32[webCamTexture.width * webCamTexture.height];
                    rgbaMat = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);
                    texture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);



                    detector = new ColorBlobDetector();
                    spectrumMat = new Mat();
                    //										blobColorRgba = new Scalar (255);
                    blobColorHsv = new Scalar(255);
                    SPECTRUM_SIZE = new Size(200, 64);
                    CONTOUR_COLOR = new Scalar(255, 0, 0, 255);
                    CONTOUR_COLOR_WHITE = new Scalar(255, 255, 255, 255);



                    gameObject.GetComponent<Renderer>().material.mainTexture = texture;

                    updateLayout();

                    screenOrientation = Screen.orientation;
                    initDone = true;

                    break;
                }
                else
                {
                    yield return 0;
                }
            }
        }

        private void updateLayout()
        {
            gameObject.transform.localRotation = new Quaternion(0, 0, 0, 0);
            gameObject.transform.localScale = new Vector3(webCamTexture.width, webCamTexture.height, 1);

            if (webCamTexture.videoRotationAngle == 90 || webCamTexture.videoRotationAngle == 270)
            {
                gameObject.transform.eulerAngles = new Vector3(0, 0, -90);
            }


            float width = 0;
            float height = 0;
            if (webCamTexture.videoRotationAngle == 90 || webCamTexture.videoRotationAngle == 270)
            {
                width = gameObject.transform.localScale.y;
                height = gameObject.transform.localScale.x;
            }
            else if (webCamTexture.videoRotationAngle == 0 || webCamTexture.videoRotationAngle == 180)
            {
                width = gameObject.transform.localScale.x;
                height = gameObject.transform.localScale.y;
            }

            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale)
            {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            }
            else
            {
                Camera.main.orthographicSize = height / 2;
            }
        }


        // Update is called once per frame
        void Update()
        {
            if (!initDone)
                return;


            if (screenOrientation != Screen.orientation)
            {
                screenOrientation = Screen.orientation;
                updateLayout();
            }


#if UNITY_IOS && !UNITY_EDITOR && (UNITY_4_6_3 || UNITY_4_6_4 || UNITY_5_0_0 || UNITY_5_0_1)
				        if (webCamTexture.width > 16 && webCamTexture.height > 16) {
#else
            if (webCamTexture.didUpdateThisFrame)
            {
#endif

                Utils.webCamTextureToMat(webCamTexture, rgbaMat, colors);

                if (webCamDevice.isFrontFacing)
                {
                    if (webCamTexture.videoRotationAngle == 0)
                    {
                        Core.flip(rgbaMat, rgbaMat, 1);
                    }
                    else if (webCamTexture.videoRotationAngle == 90)
                    {
                        Core.flip(rgbaMat, rgbaMat, 0);
                    }
                    if (webCamTexture.videoRotationAngle == 180)
                    {
                        Core.flip(rgbaMat, rgbaMat, 0);
                    }
                    else if (webCamTexture.videoRotationAngle == 270)
                    {
                        Core.flip(rgbaMat, rgbaMat, 1);
                    }
                }
                else
                {
                    if (webCamTexture.videoRotationAngle == 180)
                    {
                        Core.flip(rgbaMat, rgbaMat, -1);
                    }
                    else if (webCamTexture.videoRotationAngle == 270)
                    {
                        Core.flip(rgbaMat, rgbaMat, -1);
                    }
                }



#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
						//Touch
						int touchCount = Input.touchCount;
						if (touchCount == 1)
						{
							Touch t = Input.GetTouch(0);
							if(t.phase == TouchPhase.Ended){
								onTouch(convertScreenPoint (new Point (t.position.x, t.position.y), gameObject, Camera.main));
								//									Debug.Log ("touch X " + t.position.x);
								//									Debug.Log ("touch Y " + t.position.y);
							}
						}
#else
                //Mouse
                if (Input.GetMouseButtonUp(0))
                {

                    onTouch(convertScreenPoint(new Point(Input.mousePosition.x, Input.mousePosition.y), gameObject, Camera.main));
                    //												Debug.Log ("mouse X " + Input.mousePosition.x);
                    //												Debug.Log ("mouse Y " + Input.mousePosition.y);
                }
#endif


                handPoseEstimationProcess();

                Core.putText(rgbaMat, "PLEASE TOUCH HAND POINTS", new Point(5, rgbaMat.rows() - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(255, 255, 255, 255), 2, Core.LINE_AA, false);


                Utils.matToTexture2D(rgbaMat, texture, colors);
            }

        }

        void OnDisable()
        {
            webCamTexture.Stop();
        }

        public void OnDropDown(int selec)
        {
            handmode = selec;
        }


        public void OnBackButton()
        {
            Application.LoadLevel("OpenCVForUnitySample");
        }

        public void OnChangeCameraButton()
        {
            shouldUseFrontFacing = !shouldUseFrontFacing;
            StartCoroutine(init());
        }

        /// <summary>
        /// Hands the pose estimation process.
        /// </summary>
        public void handPoseEstimationProcess()
        {
            List<Point> firstpoint = new List<Point>();
            List<Point> secondpoint = new List<Point>();



            //Imgproc.blur(mRgba, mRgba, new Size(5,5));
            Imgproc.GaussianBlur(rgbaMat, rgbaMat, new OpenCVForUnity.Size(3, 3), 1, 1);
            //Imgproc.medianBlur(mRgba, mRgba, 3);

            if (!isColorSelected)
                return;

            List<MatOfPoint> contours = detector.getContours();
            detector.process(rgbaMat);

            //						Debug.Log ("Contours count: " + contours.Count);

            if (contours.Count <= 0)
            {
                return;
            }

            RotatedRect rect = Imgproc.minAreaRect(new MatOfPoint2f(contours[0].toArray()));

            double boundWidth = rect.size.width;
            double boundHeight = rect.size.height;
            int boundPos = 0;

            for (int i = 1; i < contours.Count; i++)
            {
                rect = Imgproc.minAreaRect(new MatOfPoint2f(contours[i].toArray()));
                if (rect.size.width * rect.size.height > boundWidth * boundHeight)
                {
                    boundWidth = rect.size.width;
                    boundHeight = rect.size.height;
                    boundPos = i;
                }
            }

            OpenCVForUnity.Rect boundRect = Imgproc.boundingRect(new MatOfPoint(contours[boundPos].toArray()));
            Core.rectangle(rgbaMat, boundRect.tl(), boundRect.br(), CONTOUR_COLOR_WHITE, 2, 8, 0);

            //						Debug.Log (
            //						" Row start [" + 
            //								(int)boundRect.tl ().y + "] row end [" +
            //								(int)boundRect.br ().y + "] Col start [" +
            //								(int)boundRect.tl ().x + "] Col end [" +
            //								(int)boundRect.br ().x + "]");


            double a = boundRect.br().y - boundRect.tl().y;
            //a = a * 0.7;
            a = a * 0.85;
            a = boundRect.tl().y + a;

            //						Debug.Log (
            //						" A [" + a + "] br y - tl y = [" + (boundRect.br ().y - boundRect.tl ().y) + "]");

            //Core.rectangle( mRgba, boundRect.tl(), boundRect.br(), CONTOUR_COLOR, 2, 8, 0 );
            Core.rectangle(rgbaMat, boundRect.tl(), new Point(boundRect.br().x, a), CONTOUR_COLOR, 2, 8, 0);
            //Core.rectangle(rgbaMat, boundRect.tl(), new Point(boundRect.br().x, a), CONTOUR_COLOR, 2, 8, 0);


            MatOfPoint2f pointMat = new MatOfPoint2f();
            Imgproc.approxPolyDP(new MatOfPoint2f(contours[boundPos].toArray()), pointMat, 3, true);
            contours[boundPos] = new MatOfPoint(pointMat.toArray());

            MatOfInt hull = new MatOfInt();
            MatOfInt4 convexDefect = new MatOfInt4();
            Imgproc.convexHull(new MatOfPoint(contours[boundPos].toArray()), hull);

            if (hull.toArray().Length < 10)
                return;

            Imgproc.convexityDefects(new MatOfPoint(contours[boundPos].toArray()), hull, convexDefect);

            List<MatOfPoint> hullPoints = new List<MatOfPoint>();
            List<Point> listPo = new List<Point>();
            for (int j = 0; j < hull.toList().Count; j++)
            {
                listPo.Add(contours[boundPos].toList()[hull.toList()[j]]);
            }

            MatOfPoint e = new MatOfPoint();
            e.fromList(listPo);
            hullPoints.Add(e);

            List<MatOfPoint> defectPoints = new List<MatOfPoint>();
            List<Point> listPoDefect = new List<Point>();
            for (int j = 0; j < convexDefect.toList().Count; j = j + 4)
            {
                Point farPoint = contours[boundPos].toList()[convexDefect.toList()[j + 2]];
                int depth = convexDefect.toList()[j + 3];
                if (depth > threasholdSlider.value && farPoint.y < a)
                {
                    listPoDefect.Add(contours[boundPos].toList()[convexDefect.toList()[j + 2]]);
                }
                //								Debug.Log ("defects [" + j + "] " + convexDefect.toList () [j + 3]);
            }

            MatOfPoint e2 = new MatOfPoint();
            e2.fromList(listPo);
            defectPoints.Add(e2);

            //						Debug.Log ("hull: " + hull.toList ());
            //						Debug.Log ("defects: " + convexDefect.toList ());


            Imgproc.drawContours(rgbaMat, contours, -1, CONTOUR_COLOR, 3);
            Imgproc.drawContours(rgbaMat, hullPoints, -1, CONTOUR_COLOR, 3);
            string imsi = null;
            List<Point> aa;
            for (int i = 0; i < hullPoints.Count; i++) //손가락끝잡는곳
            {
                aa = hullPoints[i].toList();
                imsi += "i=" + i;
                for (int j = 0; j < aa.Count; j++)
                {
                    imsi += "(" + aa[j].x + "," + aa[j].y + ")";
                    Core.putText(rgbaMat, "" + j, aa[j], 1, 2, new Scalar(0, 0, 255, 255));
                }

                Core.circle(rgbaMat, aa[0], 3, new Scalar(0, 0, 0, 255));
                firstpoint.Add(aa[0]);
                int handpoint = 1;
                if (distancePoint(aa[0], aa[aa.Count - 1]) > 30)
                {
                    firstpoint.Add(aa[aa.Count - 1]);
                    Core.circle(rgbaMat, aa[aa.Count - 1], 3, new Scalar(0, 0, 0, 255));
                    handpoint++;
                }
                for (int j = aa.Count - 2; j >= 0; j--)
                {
                    if (distancePoint(aa[j], aa[j + 1]) > 30)
                    {
                        firstpoint.Add(aa[j]);
                        Core.circle(rgbaMat, aa[j], 3, new Scalar(0, 0, 0, 255));
                        handpoint++;
                        if (handpoint == 5)
                        {
                            break;
                        }
                    }
                }


                

            }
            //Debug.Log(imsi);

            //						int defectsTotal = (int)convexDefect.total ();
            //						Debug.Log ("Defect total " + defectsTotal);

            this.numberOfFingers = listPoDefect.Count;
            //if (this.numberOfFingers > 5)
            //		this.numberOfFingers = 5;

            // Debug.Log("numberOfFingers " + numberOfFingers);

            //						Core.putText (mRgba, "" + numberOfFingers, new Point (mRgba.cols () / 2, mRgba.rows () / 2), Core.FONT_HERSHEY_PLAIN, 4.0, new Scalar (255, 255, 255, 255), 6, Core.LINE_AA, false);
            numberOfFingersText.text = numberOfFingers.ToString();

            /*
            foreach (Point p in listPoDefect) {
                    Core.circle (rgbaMat, p, 6, new Scalar (255, 0, 255, 255), -1);
                    Core.putText(rgbaMat,""+)
            }
            */
            for (int i = 0; i < listPoDefect.Count; i++)
            {
                Core.circle(rgbaMat, listPoDefect[i], 6, new Scalar(255, 0, 255, 255), -1);
                Core.putText(rgbaMat, "" + i, listPoDefect[i], 1, 3, new Scalar(0, 255, 0, 255));
            }

            
            if (listPoDefect.Count == 6 && handmode==0) //left fore
            {
                secondpoint.Add(new Point(2*listPoDefect[0].x-listPoDefect[1].x, 2 * listPoDefect[0].y - listPoDefect[1].y));
                secondpoint.Add(new Point(2 * listPoDefect[2].x - listPoDefect[1].x, 2 * listPoDefect[2].y - listPoDefect[1].y));

                secondpoint.Add(new Point(listPoDefect[4].x + (listPoDefect[5].x-listPoDefect[4].x)*0.166f, listPoDefect[4].y + (listPoDefect[5].y - listPoDefect[4].y) * 0.166f));
                secondpoint.Add(new Point(listPoDefect[4].x + (listPoDefect[5].x - listPoDefect[4].x) * 0.5f, listPoDefect[4].y + (listPoDefect[5].y - listPoDefect[4].y) * 0.5f));
                secondpoint.Add(new Point(listPoDefect[4].x + (listPoDefect[5].x - listPoDefect[4].x) * 0.833f, listPoDefect[4].y + (listPoDefect[5].y - listPoDefect[4].y) * 0.833f));


                if (firstpoint.Count >= 5)
                {
                    secondpoint.Add(new Point(firstpoint[4].x + (secondpoint[2].x - firstpoint[4].x) * 0.75f, firstpoint[4].y + (secondpoint[2].y - firstpoint[4].y) * 0.75f));
                }
                //f4 s2 //
                //secondpoint.Add(new Point(secondpoint[3].x + (listPoDefect[3].x - secondpoint[3].x) * 0.666f, secondpoint[3].y + (listPoDefect[3].y - secondpoint[3].y) * 0.666f));
                //se2랑 3골짜기 사이
                secondpoint.Add(new Point(secondpoint[2].x + (listPoDefect[1].x - secondpoint[2].x) * 0.666f, secondpoint[2].y + (listPoDefect[1].y - secondpoint[2].y) * 0.666f));
                //se1랑 1골짜기 사이
                secondpoint.Add(new Point(listPoDefect[0].x + (secondpoint[4].x - listPoDefect[0].x) * 0.333f, listPoDefect[0].y + (secondpoint[4].y - listPoDefect[0].y) * 0.333f));
                //0골짜기 se4사이 0.3
            }
            else if  (listPoDefect.Count == 6 && handmode==1) //left back
            {
                secondpoint.Add(new Point(2 * listPoDefect[1].x - listPoDefect[2].x, 2 * listPoDefect[1].y - listPoDefect[2].y));
                secondpoint.Add(new Point(2 * listPoDefect[3].x - listPoDefect[2].x, 2 * listPoDefect[3].y - listPoDefect[2].y));


                secondpoint.Add(new Point(listPoDefect[4].x + (listPoDefect[5].x - listPoDefect[4].x) * 0.125f, listPoDefect[4].y + (listPoDefect[5].y - listPoDefect[4].y) * 0.125f)); //손목3개
                secondpoint.Add(new Point(listPoDefect[4].x + (listPoDefect[5].x - listPoDefect[4].x) * 0.333f, listPoDefect[4].y + (listPoDefect[5].y - listPoDefect[4].y) * 0.333f));
                secondpoint.Add(new Point(listPoDefect[4].x + (listPoDefect[5].x - listPoDefect[4].x) * 0.833f, listPoDefect[4].y + (listPoDefect[5].y - listPoDefect[4].y) * 0.833f));

                secondpoint.Add(new Point(secondpoint[1].x + (secondpoint[2].x - secondpoint[1].x) * 0.2854f, secondpoint[1].y + (secondpoint[2].y - secondpoint[1].y) * 0.2854f));
                //se1이랑 se2이랑 0.2854
                secondpoint.Add(new Point(secondpoint[2].x + (listPoDefect[3].x - secondpoint[2].x) * 0.777f, secondpoint[2].y + (listPoDefect[3].y - secondpoint[2].y) * 0.777f));
                //se2이랑 골짜기3 0.777
                secondpoint.Add(new Point(secondpoint[2].x + (listPoDefect[3].x - secondpoint[2].x) * 0.111f, secondpoint[2].y + (listPoDefect[3].y - secondpoint[2].y) * 0.111f));
                //se2이랑 골짜기3 0.111

                //se4이랑 se0이랑 0.777
                secondpoint.Add(new Point(secondpoint[4].x + (secondpoint[0].x - secondpoint[4].x) * 0.777f, secondpoint[4].y + (secondpoint[0].y - secondpoint[4].y) * 0.777f));
                //se4이랑 se0이랑 0.444
                secondpoint.Add(new Point(secondpoint[4].x + (secondpoint[0].x - secondpoint[4].x) * 0.444f, secondpoint[4].y + (secondpoint[0].y - secondpoint[4].y) * 0.444f));
            }
            else if (listPoDefect.Count == 6 && handmode == 2) //right fore
            {
                secondpoint.Add(new Point(2 * listPoDefect[1].x - listPoDefect[2].x, 2 * listPoDefect[1].y - listPoDefect[2].y));
                secondpoint.Add(new Point(2 * listPoDefect[3].x - listPoDefect[2].x, 2 * listPoDefect[3].y - listPoDefect[2].y));


                secondpoint.Add(new Point(listPoDefect[4].x + (listPoDefect[5].x - listPoDefect[4].x) * 0.166f, listPoDefect[4].y + (listPoDefect[5].y - listPoDefect[4].y) * 0.166f)); //손목3개
                secondpoint.Add(new Point(listPoDefect[4].x + (listPoDefect[5].x - listPoDefect[4].x) * 0.5f, listPoDefect[4].y + (listPoDefect[5].y - listPoDefect[4].y) * 0.5f));
                secondpoint.Add(new Point(listPoDefect[4].x + (listPoDefect[5].x - listPoDefect[4].x) * 0.833f, listPoDefect[4].y + (listPoDefect[5].y - listPoDefect[4].y) * 0.833f));

                secondpoint.Add(new Point(listPoDefect[3].x + (secondpoint[2].x - listPoDefect[3].x) * 0.333f, listPoDefect[3].y + (secondpoint[2].y - listPoDefect[3].y) * 0.333f));
                //골짜기3 se2 0.333
                secondpoint.Add(new Point(listPoDefect[1].x + (secondpoint[3].x - listPoDefect[1].x) * 0.333f, listPoDefect[1].y + (secondpoint[3].y - listPoDefect[1].y) * 0.333f));
                //골짜기1 se3 0.333
                secondpoint.Add(new Point(firstpoint[0].x + (secondpoint[4].x - firstpoint[0].x) * 0.75f, firstpoint[0].y + (secondpoint[4].y - firstpoint[0].y) * 0.75f));
                //fp0 se4 0.75
            }
            else if (listPoDefect.Count == 6 && handmode == 3) //right back
            {
                secondpoint.Add(new Point(2 * listPoDefect[0].x - listPoDefect[1].x, 2 * listPoDefect[0].y - listPoDefect[1].y));
                secondpoint.Add(new Point(2 * listPoDefect[2].x - listPoDefect[1].x, 2 * listPoDefect[2].y - listPoDefect[1].y));

                secondpoint.Add(new Point(listPoDefect[4].x + (listPoDefect[5].x - listPoDefect[4].x) * 0.166f, listPoDefect[4].y + (listPoDefect[5].y - listPoDefect[4].y) * 0.166f));
                secondpoint.Add(new Point(listPoDefect[4].x + (listPoDefect[5].x - listPoDefect[4].x) * 0.666f, listPoDefect[4].y + (listPoDefect[5].y - listPoDefect[4].y) * 0.666f));
                secondpoint.Add(new Point(listPoDefect[4].x + (listPoDefect[5].x - listPoDefect[4].x) * 0.875f, listPoDefect[4].y + (listPoDefect[5].y - listPoDefect[4].y) * 0.875f));

                secondpoint.Add(new Point(secondpoint[1].x + (secondpoint[2].x - secondpoint[1].x) * 0.222f, secondpoint[1].y + (secondpoint[2].y - secondpoint[1].y) * 0.222f));
                //s1 s2 0.222
                secondpoint.Add(new Point(secondpoint[1].x + (secondpoint[2].x - secondpoint[1].x) * 0.555f, secondpoint[1].y + (secondpoint[2].y - secondpoint[1].y) * 0.555f));
                //s1 s2 0.555

                secondpoint.Add(new Point(listPoDefect[0].x + (secondpoint[4].x - listPoDefect[0].x) * 0.222f, listPoDefect[0].y + (secondpoint[4].y - listPoDefect[0].y) * 0.222f));
                //골짜기0 s4 0.222
                secondpoint.Add(new Point(listPoDefect[0].x + (secondpoint[4].x - listPoDefect[0].x) * 0.888f, listPoDefect[0].y + (secondpoint[4].y - listPoDefect[0].y) * 0.888f));
                //골짜기0 s4 0.888
                secondpoint.Add(new Point(secondpoint[0].x + (secondpoint[4].x - secondpoint[0].x) * 0.286f, secondpoint[0].y + (secondpoint[4].y - secondpoint[0].y) * 0.286f));
                //s0 s4 0.286

            }
            string[][] imsiname = {
                new string[] { "", "", "", "", "", "", "", "", "", ""},
                new string[] { "", "", "", "", "", "", "", "", "", "" },
                new string[] { "", "", "HT7", "PC7", "LU9", "HT9", "PC8", "LU10", "", "" },
                new string[] { "SI2", "LI2", "LI5", "T4", "SI5", "LI3", "LI4", "TE3", "SI5", "" }
            };
            for (int i = 0; i < secondpoint.Count; i++)
            {
                Core.circle(rgbaMat, secondpoint[i], 6, new Scalar(255, 128, 0, 255), -1);
                if (imsiname[handmode][i] == "")
                {
                    Core.putText(rgbaMat, "S" + i, secondpoint[i], 1, 2, new Scalar(255, 0, 255, 255));
                }
                else
                {
                    Core.putText(rgbaMat, "S"+imsiname[handmode][i], secondpoint[i], 1, 2, new Scalar(0, 255, 0, 255));
                }


            }


        }

        /// <summary>
        /// Ons the touch.
        /// </summary>
        /// <param name="touchPoint">Touch point.</param>
        /// 
        public void wikibutton()
        {
            Application.OpenURL("http://www.kmcric.com");
        }

        public void onTouch(Point touchPoint)
        {

            int cols = rgbaMat.cols();
            int rows = rgbaMat.rows();

            int x = (int)touchPoint.x;
            int y = (int)touchPoint.y;

            //						Debug.Log ("Touch image coordinates: (" + x + ", " + y + ")");

            if ((x < 0) || (y < 0) || (x > cols) || (y > rows))
                return;

            OpenCVForUnity.Rect touchedRect = new OpenCVForUnity.Rect();

            touchedRect.x = (x > 5) ? x - 5 : 0;
            touchedRect.y = (y > 5) ? y - 5 : 0;

            touchedRect.width = (x + 5 < cols) ? x + 5 - touchedRect.x : cols - touchedRect.x;
            touchedRect.height = (y + 5 < rows) ? y + 5 - touchedRect.y : rows - touchedRect.y;

            Mat touchedRegionRgba = rgbaMat.submat(touchedRect);

            Mat touchedRegionHsv = new Mat();
            Imgproc.cvtColor(touchedRegionRgba, touchedRegionHsv, Imgproc.COLOR_RGB2HSV_FULL);

            // Calculate average color of touched region
            blobColorHsv = Core.sumElems(touchedRegionHsv);
            int pointCount = touchedRect.width * touchedRect.height;
            for (int i = 0; i < blobColorHsv.val.Length; i++)
                blobColorHsv.val[i] /= pointCount;

            //						blobColorRgba = converScalarHsv2Rgba (blobColorHsv);					
            //						Debug.Log ("Touched rgba color: (" + mBlobColorRgba.val [0] + ", " + mBlobColorRgba.val [1] +
            //								", " + mBlobColorRgba.val [2] + ", " + mBlobColorRgba.val [3] + ")");

            detector.setHsvColor(blobColorHsv);

            Imgproc.resize(detector.getSpectrum(), spectrumMat, SPECTRUM_SIZE);

            isColorSelected = true;

            touchedRegionRgba.release();
            touchedRegionHsv.release();
        }

        /// <summary>
        /// Convers the scalar hsv2 rgba.
        /// </summary>
        /// <returns>The scalar hsv2 rgba.</returns>
        /// <param name="hsvColor">Hsv color.</param>
        private Scalar converScalarHsv2Rgba(Scalar hsvColor)
        {
            Mat pointMatRgba = new Mat();
            Mat pointMatHsv = new Mat(1, 1, CvType.CV_8UC3, hsvColor);
            Imgproc.cvtColor(pointMatHsv, pointMatRgba, Imgproc.COLOR_HSV2RGB_FULL, 4);

            return new Scalar(pointMatRgba.get(0, 0));
        }

        /// <summary>
        /// Converts the screen point.
        /// </summary>
        /// <returns>The screen point.</returns>
        /// <param name="screenPoint">Screen point.</param>
        /// <param name="quad">Quad.</param>
        /// <param name="cam">Cam.</param>
        static Point convertScreenPoint(Point screenPoint, GameObject quad, Camera cam)
        {
            Vector2 tl;
            Vector2 tr;
            Vector2 br;
            Vector2 bl;

            if (Input.deviceOrientation == DeviceOrientation.Portrait || Input.deviceOrientation == DeviceOrientation.PortraitUpsideDown)
            {
                tl = cam.WorldToScreenPoint(new Vector3(quad.transform.localPosition.x + quad.transform.localScale.y / 2, quad.transform.localPosition.y + quad.transform.localScale.x / 2, quad.transform.localPosition.z));
                tr = cam.WorldToScreenPoint(new Vector3(quad.transform.localPosition.x + quad.transform.localScale.y / 2, quad.transform.localPosition.y - quad.transform.localScale.x / 2, quad.transform.localPosition.z));
                br = cam.WorldToScreenPoint(new Vector3(quad.transform.localPosition.x - quad.transform.localScale.y / 2, quad.transform.localPosition.y - quad.transform.localScale.x / 2, quad.transform.localPosition.z));
                bl = cam.WorldToScreenPoint(new Vector3(quad.transform.localPosition.x - quad.transform.localScale.y / 2, quad.transform.localPosition.y + quad.transform.localScale.x / 2, quad.transform.localPosition.z));
            }
            else
            {
                tl = cam.WorldToScreenPoint(new Vector3(quad.transform.localPosition.x - quad.transform.localScale.x / 2, quad.transform.localPosition.y + quad.transform.localScale.y / 2, quad.transform.localPosition.z));
                tr = cam.WorldToScreenPoint(new Vector3(quad.transform.localPosition.x + quad.transform.localScale.x / 2, quad.transform.localPosition.y + quad.transform.localScale.y / 2, quad.transform.localPosition.z));
                br = cam.WorldToScreenPoint(new Vector3(quad.transform.localPosition.x + quad.transform.localScale.x / 2, quad.transform.localPosition.y - quad.transform.localScale.y / 2, quad.transform.localPosition.z));
                bl = cam.WorldToScreenPoint(new Vector3(quad.transform.localPosition.x - quad.transform.localScale.x / 2, quad.transform.localPosition.y - quad.transform.localScale.y / 2, quad.transform.localPosition.z));
            }

            Mat srcRectMat = new Mat(4, 1, CvType.CV_32FC2);
            Mat dstRectMat = new Mat(4, 1, CvType.CV_32FC2);


            srcRectMat.put(0, 0, tl.x, tl.y, tr.x, tr.y, br.x, br.y, bl.x, bl.y);
            dstRectMat.put(0, 0, 0.0, 0.0, quad.transform.localScale.x, 0.0, quad.transform.localScale.x, quad.transform.localScale.y, 0.0, quad.transform.localScale.y);


            Mat perspectiveTransform = Imgproc.getPerspectiveTransform(srcRectMat, dstRectMat);

            //						Debug.Log ("srcRectMat " + srcRectMat.dump ());
            //						Debug.Log ("dstRectMat " + dstRectMat.dump ());
            //						Debug.Log ("perspectiveTransform " + perspectiveTransform.dump ());

            MatOfPoint2f srcPointMat = new MatOfPoint2f(screenPoint);
            MatOfPoint2f dstPointMat = new MatOfPoint2f();

            Core.perspectiveTransform(srcPointMat, dstPointMat, perspectiveTransform);

            //						Debug.Log ("srcPointMat " + srcPointMat.dump ());
            //						Debug.Log ("dstPointMat " + dstPointMat.dump ());

            return dstPointMat.toArray()[0];
        }
        private float distancePoint(Point a, Point b)
        {
            float rt = Mathf.Sqrt((float)((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y)));
            return rt;
        }
    }
}