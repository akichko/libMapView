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

        //ViewParam viewParam;

        public LatLon selectedLatLon;

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


        //public void SetViewParam(ViewParam viewParam)
        //{
        //    this.viewParam = viewParam;
        //}

        public void DrawTile(Graphics g, List<CmnTile> tileList, ViewParam viewParam, UInt16 objType )
        {

            //Graphics g2 = viewAccess.GetDrawAreaGraphics();
            Bitmap bitmap = viewParam.CreateBitmap();

            //Bitmap daBitmap = viewAccess.GetDrawAreaImage();
            Graphics g2 = Graphics.FromImage(bitmap);

            //各タイルを描画

            //this.viewParam = viewParam;


            //コールバック用ローカル関数定義
            int CbDrawFunc(CmnObj cmnObj)
            {
                return DrawMapObj(g2, cmnObj, viewParam);
                //return DrawMapObj(g, viewParam, cmnObj);
            }

            foreach (CmnTile drawTile in tileList)
            {
                //各オブジェクト描画
                drawTile.DrawData(new CbGetObjFunc(CbDrawFunc), objType);
                //タイル枠描画
                drawApi.DrawObj(g2, (CmnObj)drawTile, viewParam);
                //drawTile.DrawData(null, new CbGetObjFunc(CbDrawFunc));
                //drawApi.DrawPolyline(g2, viewParam, drawTile.tileInfo.GetGeometry());
            }

            //座標点追加描画
            drawApi.DrawPoint(g2, selectedLatLon, viewParam);


            viewAccess.UpdateImage(bitmap);
            
        }


        public int DrawMapObj(Graphics g, CmnObj cmnObj, ViewParam viewParam)
        {
            drawApi.DrawObj(g, cmnObj, viewParam);

            return 0;
        }


        public void RefreshDrawArea()
        {
            viewAccess.RefreshDrawArea();
        }


        public void ShowAttribute(CmnObjHandle objHdl)
        {
            viewAccess.DispListView(objHdl.obj.GetAttributeListItem(objHdl.tile));

        }

        public void SetSelectedObj(CmnObj mapLink)
        {
            drawApi.selectObj = mapLink;

        }

        public void SetRelatedObj(List<CmnObjHdlRef> refObjList)
        {
            drawApi.refObjList = refObjList;
        }

        public void UpdateCenterLatLon(LatLon latlon)
        {
            viewAccess.DispCenterLatLon(latlon);
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



    public interface IViewApi
    {
    //    Graphics GetDrawAreaGraphics();
    //    void SetDrawAreaImage(Bitmap bmp);

        void RefreshDrawArea();
        void DispCurrentTileId(uint tileId);
        void DispListView(List<AttrItemInfo> listItem);

        void DispCenterLatLon(LatLon latlon);
        //ViewModel GetViewModel();

        //Image GetDrawAreaImage();

        void UpdateImage(Image newImage);

        //Route
        //void DispDest(string destStr);
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


    //描画用抽象クラス
    public abstract class CmnDrawApi
    {
        public CmnObj selectObj = null;

        public List<CmnObjHdlRef> refObjList = null;

        public LatLon selectPoint;


        //オブジェクト描画
        public virtual void DrawObj(Graphics g, CmnObj obj, ViewParam viewParam)
        {
            PointF[] pointF = CalcPolylineInDrawArea(obj.Geometry, viewParam);

            Pen pen = GetPen(obj);
            g.DrawLines(pen, pointF);

            return;
        }
    
        //デフォルト描画スタイル
        public virtual Pen GetPen(CmnObj obj)
        {
            if (ReferenceEquals(selectObj, obj))
                return new Pen(Color.Red, (float)4.0);

            if (refObjList?.Count(x => ReferenceEquals(x.objHdl.obj, obj)) > 0)
                return new Pen(Color.DarkGreen, (float)4.0);

            return new Pen(Color.Black, 1);
        }

        //ポイント描画
        public virtual void DrawPoint(Graphics g, LatLon latlon, ViewParam viewParam)
        {
            if (latlon == null)
                return;
            PointF pointF = CalcPointInDrawArea(latlon, viewParam);

            //色は暫定
            float width = 6;
            g.FillEllipse(new SolidBrush(Color.DodgerBlue), pointF.X - width, pointF.Y - width, width*2, width*2);
            width = 3;
            g.FillEllipse(new SolidBrush(Color.Yellow), pointF.X - width, pointF.Y - width, width * 2, width * 2);

        }


        protected PointF CalcPointInDrawArea(LatLon latlon, ViewParam viewParam)
        {
            //相対緯度経度算出
            double relLat = latlon.lat - viewParam.viewCenter.lat;
            double relLon = latlon.lon - viewParam.viewCenter.lon;

            return new PointF((float)(viewParam.width / 2.0 + relLon * viewParam.GetDotPerLon()), (float)(viewParam.height / 2.0 - relLat * viewParam.GetDotPerLat()));

        }

        protected PointF[] CalcPolylineInDrawArea(LatLon[] geometry, ViewParam viewParam)
        {
            return geometry.Select(x => CalcPointInDrawArea(x, viewParam)).ToArray();
        }

        //public int DrawPolyline(Graphics g, ViewParam viewParam, LatLon[] geometry)
        //{
        //    PointF[] pointF = new PointF[geometry.Length];

        //    for (int i = 0; i < geometry.Length; i++)
        //    {
        //        pointF[i] = CalcPointInDrawArea(geometry[i], viewParam);
        //    }

        //    Pen pen = new Pen(Color.Black);
        //    g.DrawLines(pen, pointF);
        //    return 0;

        //}
    }




}
