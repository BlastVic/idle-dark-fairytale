
/*
 * Enabling this define call UpdateSorting from OnWillRenderObject event, instead finding if there is any render part visible for each IsoSpriteSorting object.
 * If this is enabled, the object containing IsoSpriteSorting script should have a render so OnWillRenderObject is called.
 * I have seen no performance improvement disabling this define, so I have left this commented to make it optional placing the IsoSpriteSorting script in an object with a renderer.
 */
//#define UPDATE_SORTING_ON_WILL_RENDER_OBJECT

using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace CreativeSpore.SpriteSorting
{

    [ExecuteInEditMode]
    public class IsoSpriteSorting : MonoBehaviour
    {
        //NOTE: set to -32768 instead if you are displaying more than 32767 sprites to allow sorting properly another 32768 extra sprites.
        // Or set to 0 to see the order number better (starting with 0) as so many sprites at the same time would be crazy
        public const int k_BaseSortingValue = -32768;
        public const string k_IntParam_ExecuteInEditMode = "IsoSpriteSorting_ExecuteInEditMode";

        /// <summary>
        /// Data for each separated axis
        /// </summary>
        class AxisData
        {
            public List<IsoSpriteSorting> ListSortedIsoSprs = new List<IsoSpriteSorting>();
            public int OrderCnt = k_BaseSortingValue;
            public int LastFrameCnt = 0;
        };

        /// <summary>
        /// Sorting axis
        /// </summary>
        public enum eSortingAxis
        {
            X,
            Y,
            Z,
            XY,
            YZ,
            ZX,
            CameraForward
        }

        static Dictionary<IsoSpriteSorting, Renderer[]> s_dicInstanceSpriteList = new Dictionary<IsoSpriteSorting, Renderer[]>();


        /// <summary>
        /// Separation of management by sorting axis
        /// </summary>
        static AxisData[] s_axisData = null;

        /// <summary>
        /// Sorting axis
        /// </summary>
        public eSortingAxis SorterAxis = eSortingAxis.Y;
        public Vector3 SorterPositionOffset = new Vector3();

        /// <summary>
        /// If invalidated and IncludeInactiveRenderer is true, inactive renderers will be taking into account
        /// </summary>
        public bool IncludeInactiveRenderer = true;

        private bool m_invalidated = true;

        /// <summary>
        /// Invalidate when adding or removing any renderer
        /// </summary>
        public void Invalidate()
        {
            m_invalidated = true;
        }

        /// <summary>
        /// Invalidate all objects
        /// </summary>
        public void InvalidateAll()
        {
            foreach( IsoSpriteSorting obj in s_dicInstanceSpriteList.Keys )
            {
                obj.Invalidate();
            }
        }

        public string GetStatistics()
        {
            string sStats = "";
            
            int nbOfVisibleObjs = 0;
            int nbOfRenderers = 0;

            sStats = "<b>Stats By Sorting Axis:</b>\n";
            foreach( eSortingAxis sortingAxis in System.Enum.GetValues(typeof(eSortingAxis)) )
            {
                sStats += "  <b>- " + sortingAxis + ":</b>\n";
                if (s_axisData[(int)sortingAxis].ListSortedIsoSprs.Count > 0)
                {
                    int orderCntRelToBase = s_axisData[(int)sortingAxis].OrderCnt - k_BaseSortingValue;
                    nbOfVisibleObjs += s_axisData[(int)sortingAxis].ListSortedIsoSprs.Count;
                    nbOfRenderers += orderCntRelToBase;
                    sStats += "\tTotal Visible Objects: " + s_axisData[(int)sortingAxis].ListSortedIsoSprs.Count + "\n";
                    sStats += "\tTotal Visible Renderers: " + orderCntRelToBase + "\n";
                }
            }

            sStats += "\n<b>Global Stats:</b>\n";
            sStats += "\tTotal Registered Objects: " + s_dicInstanceSpriteList.Keys.Count + "\n";
            sStats += "\tTotal Visible Objects: " + nbOfVisibleObjs + "\n";
            sStats += "\tTotal Visible Renderers: " + nbOfRenderers + "\n";

            return sStats;
        }

        void Start()
        {
            Invalidate();
        }

        void Update()
        {

            if (m_invalidated)
            {
                m_invalidated = false;
                Renderer[] outList;
                outList = GetComponentsInChildren<Renderer>( IncludeInactiveRenderer );
                System.Array.Sort(outList, (a, b) => a.sortingOrder.CompareTo(b.sortingOrder));
                s_dicInstanceSpriteList[this] = outList;
            }

            bool isVisible = false;
#if !UPDATE_SORTING_ON_WILL_RENDER_OBJECT
            Renderer[] aSprRenderer = null;
            s_dicInstanceSpriteList.TryGetValue(this, out aSprRenderer);
            if (aSprRenderer != null)
            {
                for (int i = 0; i < aSprRenderer.Length && !isVisible; ++i)
                {
                    isVisible = (aSprRenderer[i] == null) || aSprRenderer[i].isVisible; //NOTE: if null, UpdateSorting should be called to clear null renderers
                }
            }
#endif

            if ( isVisible || !Application.isPlaying && PlayerPrefs.GetInt(IsoSpriteSorting.k_IntParam_ExecuteInEditMode, 1) != 0)
            {
                UpdateSorting();
            }
        }

        static Vector3 s_vXY = new Vector3(1f, 1f, 0f);
        static Vector3 s_vYZ = new Vector3(0f, 1f, 1f);
        static Vector3 s_vZX = new Vector3(1f, 0f, 1f);
        void UpdateSorting()
        {            
            int iSortingAxis = (int)SorterAxis;
            if(s_axisData == null || s_axisData.Length != System.Enum.GetValues(typeof(eSortingAxis)).Length)
            {
                s_axisData = new AxisData[System.Enum.GetValues(typeof(eSortingAxis)).Length];
                for(int i = 0; i < s_axisData.Length; ++i)
                {
                    s_axisData[i] = new AxisData();
                }
            }
            List<IsoSpriteSorting> listSortedIsoSpr = s_axisData[iSortingAxis].ListSortedIsoSprs;
            if (Time.frameCount != s_axisData[iSortingAxis].LastFrameCnt)
            {
                s_axisData[iSortingAxis].LastFrameCnt = Time.frameCount;
                s_axisData[iSortingAxis].OrderCnt = k_BaseSortingValue; 

                //Sort sprites
                switch (SorterAxis)
                {
                    case eSortingAxis.X: listSortedIsoSpr.Sort((a, b) => (b.SorterPositionOffset.x + b.transform.position.x).CompareTo(a.SorterPositionOffset.x + a.transform.position.x)); break;
                    case eSortingAxis.Y: listSortedIsoSpr.Sort((a, b) => (b.SorterPositionOffset.y + b.transform.position.y).CompareTo(a.SorterPositionOffset.y + a.transform.position.y)); break;
                    case eSortingAxis.Z: listSortedIsoSpr.Sort((a, b) => (b.SorterPositionOffset.z + b.transform.position.z).CompareTo(a.SorterPositionOffset.z + a.transform.position.z)); break;
                    case eSortingAxis.XY: listSortedIsoSpr.Sort((a, b) => Vector3.Dot(s_vXY, (b.SorterPositionOffset + b.transform.position) - (a.SorterPositionOffset + a.transform.position)).CompareTo(0f)); break;
                    case eSortingAxis.YZ: listSortedIsoSpr.Sort((a, b) => Vector3.Dot(s_vYZ, (b.SorterPositionOffset + b.transform.position) - (a.SorterPositionOffset + a.transform.position)).CompareTo(0f)); break;
                    case eSortingAxis.ZX: listSortedIsoSpr.Sort((a, b) => Vector3.Dot(s_vZX, (b.SorterPositionOffset + b.transform.position) - (a.SorterPositionOffset + a.transform.position)).CompareTo(0f)); break;
#if UPDATE_SORTING_ON_WILL_RENDER_OBJECT
                    case eSortingAxis.CameraForward: listSortedIsoSpr.Sort((a, b) => Vector3.Dot(Camera.current.transform.forward, (b.SorterPositionOffset + b.transform.position) - (a.SorterPositionOffset + a.transform.position)).CompareTo(0f)); break;
#else
                    case eSortingAxis.CameraForward: listSortedIsoSpr.Sort((a, b) => Vector3.Dot(Camera.main.transform.forward, (b.SorterPositionOffset + b.transform.position) - (a.SorterPositionOffset + a.transform.position)).CompareTo(0f)); break;
#endif
                }
                for (int i = 0; i < listSortedIsoSpr.Count; ++i)
                {
                    Renderer[] aSprRenderer = null;
                    listSortedIsoSpr[i].m_invalidated = !s_dicInstanceSpriteList.TryGetValue(listSortedIsoSpr[i], out aSprRenderer);
                    if (aSprRenderer != null)
                    {
                        for (int j = 0; j < aSprRenderer.Length; ++j)
                        {
                            if (aSprRenderer[j] != null)
                                aSprRenderer[j].sortingOrder = s_axisData[iSortingAxis].OrderCnt++;
                            else // a renderer was destroyed
                                listSortedIsoSpr[i].m_invalidated = true;
                        }
                    }
                }
                listSortedIsoSpr.Clear();
            }
            
            listSortedIsoSpr.Add(this);
        }

#if UPDATE_SORTING_ON_WILL_RENDER_OBJECT
        void OnWillRenderObject()
        {
            UpdateSorting();
        }
#endif

        void OnDestroy()
        {
            s_dicInstanceSpriteList.Remove(this);
            if (s_axisData != null)
            {
                s_axisData[(int)SorterAxis].ListSortedIsoSprs.Remove(this);
            }
        }
    }
}