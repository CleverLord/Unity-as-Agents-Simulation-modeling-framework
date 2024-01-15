using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;

namespace TerrainGeneration
{
    [SelectionBase]

    public class LineChart : MonoBehaviour
    {
        public TextMesh title;
        public TextMesh xAxisLabel;
        public TextMesh yAxisLabel;
        public TextMesh xAxisScaleTooltip;
        public TextMesh yAxisScaleTooltip;

        [ReadOnly]
        public float width;
        [ReadOnly]
        public float height;

        public ChartToWorldScaler chartToWorldScaler;
        [Tooltip("Object for setting chart size")]
        public GameObject viewPort;
        public GameObject viewPortBackground;


        [ReadOnlyWhenPlaying]
        public bool showBackground = true;
        public SpeciesCounter dataSource;
        public LineRenderer[] lineRenderers;

        // How many data lines the chart has
        [ReadOnly]
        public string[] dataLines;
        [ReadOnly]
        public int dataLinesCount;

        // Chart data
        private List<float>[] dataPoints;
        private float maxVisibleValue;

        // Drawing the chart
        // which data lines should the chart not show 
        public int[] hideLines; 

        [Range(1, 100)]
        public float visualUpdateDelay = 1f;

        [ReadOnly]
        public float nextRedrawTime;
        [ReadOnly]
        public float currTime;

        // Start is called before the first frame update
        void Start()
        {
            // scale chart viewport to wordl size if scaler object exists
            if (chartToWorldScaler != null)
                chartToWorldScaler.scaleViewportToWorld();
            // Initialize each element of the array with an empty list
            dataPoints = new List<float>[lineRenderers.Length];
            for (int i = 0; i < lineRenderers.Length; i++)
            {
                dataPoints[i] = new List<float>();
            }
            InitLineRenderers();
            // Get chart size from the viewport object
            width = viewPort.transform.localScale.x;
            height = viewPort.transform.localScale.y;
            // make background cover viewport
            viewPortBackground.transform.localScale = viewPort.transform.localScale;
            // enable the background object
            viewPortBackground.SetActive(showBackground);
            // Disable the viewport object
            viewPort.SetActive(false);

            // initialize setup time
            nextRedrawTime = 0;

            // get names for data lines
            dataLines = dataSource.getDataNames();

            // setup correct positions and scales for texts displayed on chart
            InitTexts();
        }

        // Update is called once per frame
        void Update()
        {
            currTime = Time.time;

            // Add new data points to chart data
            UpdateDataPoints();

            // Update axis data text every frame
            UpdateAxisDataText();

            // redraw chart every frame
            if (Time.time > nextRedrawTime)
            {
                DrawChart();
                nextRedrawTime = Time.time + visualUpdateDelay;
            }
        }

        private void InitLineRenderers()
        {
            // innitialize line renderers positions correctly in contex of the chart
            for (int idx = 0; idx < lineRenderers.Length; idx++)
            {
                // Set any additional LineRenderer properties here (material, width, etc.)
                lineRenderers[idx].transform.localPosition = viewPort.gameObject.transform.localPosition;
                lineRenderers[idx].positionCount = 0;
            }
        }

        // set correct locations for text displayed on the chart
        private void InitTexts()
        {
            // Position TextMesh objects for title and labels
            title.gameObject.transform.position = transform.position + new Vector3(width / 2, height, 0);
            xAxisLabel.gameObject.transform.position = transform.position + new Vector3(width / 2, 0, 0);
            yAxisLabel.gameObject.transform.position = transform.position + new Vector3(0, height / 2, 0);

            // Position axis scale texts
            xAxisScaleTooltip.transform.position = transform.position + new Vector3(width, 0, 0);
            yAxisScaleTooltip.transform.position = transform.position + new Vector3(0, height, 0);
        }

        // method to initialize axis data text
        private void InitAxisDataTexts()
        {
            
        }

        // method to update axis data text
        private void UpdateAxisDataText()
        {
            // if chart has no datapoints
            if (dataPoints.Length == 0)
                return;
            // Update X-axis data text based on the current width    
            xAxisScaleTooltip.text = $"{dataPoints[0].Count:F2}";

            // Update Y-axis data text based on the current maxVisibleValue
            yAxisScaleTooltip.text = $"{maxVisibleValue:F2}";
        }

        // register new data points to apropriate indexes
        private void UpdateDataPoints()
        {
            Dictionary<Species, int> newDataEntry = dataSource.getAgregatedData();

            // if no new data exists
            if (newDataEntry.Count == 0)
                return;

            for (int lineIdx = 0; lineIdx < lineRenderers.Length; lineIdx++)
            {
                if (hideLines.Contains(lineIdx))
                    continue;

                float newValue = newDataEntry.ElementAt(lineIdx).Value;
                UpdateLineDataPoints(lineIdx, newValue);
            }
        }

        private void UpdateLineDataPoints(int lineIdx, float newValue)
        {
            // add new value to line
            dataPoints[lineIdx].Add(newValue);
            lineRenderers[lineIdx].positionCount += 1;

            // if necesary set new max visible dataPoint value
            if (!hideLines.Contains(lineIdx) && maxVisibleValue < newValue)
            {
                maxVisibleValue = newValue;
            }

            // set line renderer points to apropriate chart values
            for (int dataPointIdx = 0; dataPointIdx < dataPoints[lineIdx].Count; dataPointIdx++)
            {
                float x = width * dataPointIdx / dataPoints[lineIdx].Count;
                x += lineRenderers[lineIdx].transform.localPosition.x;
                float y = height * dataPoints[lineIdx][dataPointIdx] / maxVisibleValue;
                y += lineRenderers[lineIdx].transform.localPosition.y;
                lineRenderers[lineIdx].SetPosition(dataPointIdx, new Vector3(x, y, 0));
            }
        }

        private void DrawChart()
        {
            // for each renderer draw line using dataPoints
            for (int idx = 0; idx < lineRenderers.Length; idx++)
            {
                bool nextLineState = !hideLines.Contains(idx);
                bool currLineState = lineRenderers[idx].gameObject.activeSelf;

                if (nextLineState != currLineState)
                {
                    // change line renderer active state to appropriate
                    lineRenderers[idx].gameObject.SetActive(nextLineState);

                    // if line is beeing hidden
                    if (nextLineState == false)
                    {
                        // set curr max visible value to max value of the still visible lines
                        float currMax = dataPoints
                            .SelectMany(line => line.Select(value => new { Value = value, LineIndex = Array.IndexOf(dataPoints, line)}))
                            .Where(point => !hideLines.Contains(point.LineIndex))
                            .Select(point => point.Value)
                            .DefaultIfEmpty(0) // Provide a default value (e.g., 0) if the sequence is empty
                            .Max();
                    } else {
                        // line stopped beeing hidden
                        float lineMaxVal = dataPoints[idx].Max();
                        if (lineMaxVal > maxVisibleValue)
                            maxVisibleValue = lineMaxVal;
                    }
                }
            }
        }
    }
}