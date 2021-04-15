using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.ComponentModel;
using libGis;

namespace libMapView
{
    public class Presenter : IOutputBoundary
    {
        IViewApi viewAccess;
        CmnDrawApi drawApi;
        //DrawTool drawTool;

        ViewParam viewParam;

        public Presenter(IViewApi mainForm)
        {
            //this.mainForm = (Form1)mainForm;
            viewAccess = (IViewApi)mainForm;
            //drawTool = new DrawTool();
        }

        public void SetDrawInterface(CmnDrawApi drawApi)
        {
            this.drawApi = drawApi;

        }


        public void SetViewParam(ViewParam viewParam)
        {
            this.viewParam = viewParam;
        }

        public void DrawTile(Graphics g, List<CmnTile> tileList, ViewParam viewParam, UInt16 objType )
        {

            //Graphics g2 = viewAccess.GetDrawAreaGraphics();
            Bitmap bitmap = viewParam.CreateBitmap();

            //Bitmap daBitmap = viewAccess.GetDrawAreaImage();
            Graphics g2 = Graphics.FromImage(bitmap);

            //各タイルを描画

            this.viewParam = viewParam;


            //コールバック用ローカル関数定義
            int CbDrawFunc(CmnObj cmnObj)
            {
                return DrawMapObj(g2, viewParam, cmnObj);
                //return DrawMapObj(g, viewParam, cmnObj);
            }

            foreach (CmnTile drawTile in tileList)
            {
                drawTile.DrawData(new CbGetObjFunc(CbDrawFunc));

            }

            viewAccess.UpdateImage(bitmap);
            
        }


        public int DrawMapObj(Graphics g, ViewParam viewParam, CmnObj cmnObj)
        {
            drawApi.DrawObj(g, viewParam, cmnObj);

            return 0;
        }


        public void RefreshDrawArea()
        {
            viewAccess.RefreshDrawArea();
        }


        public void ShowAttribute(CmnObj mapLink)
        {
            viewAccess.DispListView(mapLink.GetAttributeListItem());

        }

        public void SetSelectedLink(CmnObj mapLink)
        {
            drawApi.selectObj = mapLink;

        }

        public void UpdateCenterLatLon(LatLon latlon)
        {
            viewAccess.DispCenterLatLon(latlon.lat, latlon.lon);
        }

        public void UpdateCenterTileId(uint tileId)
        {
            viewAccess.DispCurrentTileId(tileId);
        }

        //public void DispDest(CmnObjHandle linkHdl)
        //{
        //    viewAccess.DispDest($"{linkHdl.tile.tileId}-{linkHdl.linkIndex}");
        //}
    }


    public class listView
    {
        Object Tag;
        int group;
        List<string[]> listItem = new List<string[]>();

    }

    public interface IViewApi
    {
    //    Graphics GetDrawAreaGraphics();
    //    void SetDrawAreaImage(Bitmap bmp);

        void RefreshDrawArea();
        void DispCurrentTileId(uint tileId);
        void DispListView(List<string[]> listItem);

        void DispCenterLatLon(double lat, double lon);
        ViewModel GetViewModel();

        Image GetDrawAreaImage();

        void UpdateImage(Image newImage);

        //Route
        void DispDest(string destStr);
    }

    public class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private LatLon _centerLatLon;
        public LatLon centerLatLon
        {
            get
            {
                return _centerLatLon;
            }
            set
            {
                _centerLatLon = value;

            }
        }

        int centerTileId;
        Image pbDrawAreaImage;
    }


    public abstract class CmnDrawApi
    {
       public CmnObj selectObj = null;
        public abstract int DrawObj(Graphics g, ViewParam viewParam, CmnObj cmnObj);

        protected PointF CalcPointInDrawArea(LatLon latlon, ViewParam viewParam)
        {
            //相対緯度経度算出
            double relLat = latlon.lat - viewParam.viewCenter.lat;
            double relLon = latlon.lon - viewParam.viewCenter.lon;

            return new PointF((float)(viewParam.width / 2.0 + relLon * viewParam.GetDotPerLon()), (float)(viewParam.height / 2.0 - relLat * viewParam.GetDotPerLat()));

        }


    }


}
