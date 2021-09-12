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
        ViewParam viewParam;

        Bitmap drawAreaBitmap;
        Graphics g;

        public InteractorSettings settings;
        public InteractorSettings Settings
        {
            get => settings;
            set {
                settings = value;
                drawApi.settings = value;
            }
        }

        //ViewParam viewParam;

        public LatLon selectedLatLon;
        public LatLon[] routeGeometry = null;
        public List<LatLon[]> boundaryList = null;

        //パラメータ
        public bool isDrawTileBorder = true;
        //public bool isDrawOneWay = true;


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

        public void SetViewSettings(InteractorSettings settings)
        {
            this.settings = settings;
            drawApi.settings = settings;
        }


        /* 描画 ******************************************************/

        //初期化
        public void InitializeGraphics(ViewParam viewParam)
        {
            drawAreaBitmap = viewParam.CreateBitmap();
            g = Graphics.FromImage(drawAreaBitmap);
            this.viewParam = viewParam;
        }

        //背景
        public void DrawBackGround()
        {
            //背景形状を描画
            if (boundaryList != null && settings.isAdminBoundaryDisp)
            {
                Pen pen = new Pen(Color.Gray, 1);
                boundaryList.ForEach(x =>
                {
                    drawApi.DrawPolyline(g, x, pen, viewParam);
                });
            }
        }

        //地図描画
        public void DrawMap(List<CmnTile> tileList, CmnObjFilter filter)
        {
            //各タイルを描画

            //コールバック用ローカル関数定義
            int CbDrawFunc(CmnObjHandle objHdl)
            {
                return DrawMapObj(g, objHdl, viewParam);
            }

            foreach (CmnTile drawTile in tileList)
            {
                //各オブジェクト描画
                drawTile.ExeDrawFunc(new CbGetObjFunc(CbDrawFunc), filter);

                //タイル枠描画
                //if (settings.isTileBorderDisp)
                //{
                //    drawApi.DrawObj(g, drawTile.ToCmnObjHandle(drawTile), viewParam);

                //}
            }
        }

        //タイル枠描画
        public void DrawTileBorder(List<CmnTile> tileList)
        {
            if (settings.isTileBorderDisp)
            {
                tileList.ForEach(x=>drawApi.DrawObj(g, x.ToCmnObjHandle(x), viewParam));
            }
            //foreach (CmnTile drawTile in tileList)
            //{
            //    if (settings.isTileBorderDisp)
            //    {
            //        drawApi.DrawObj(g, drawTile.ToCmnObjHandle(drawTile), viewParam);
            //    }
            //}
        }

        //座標点追加描画
        public void DrawPoint(LatLon latlon)
        {
            drawApi.DrawPoint(g, selectedLatLon, viewParam);
        }

        //経路計算結果描画
        public void DrawRouteGeometry()
        {
            //ルート形状描画
            Pen pen = new Pen(Color.FromArgb(96, 255, 0, 0), 20);
            pen.CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(2, 2);
            drawApi.DrawPolyline(g, routeGeometry, pen, viewParam);
        }

        //描画結果反映
        public void UpdateImage()
        {
            viewAccess.UpdateImage(drawAreaBitmap);
        }


        //描画メイン
        //public void DrawTile(List<CmnTile> tileList, ViewParam viewParam, CmnObjFilter filter)
        //{
        //    //Graphics初期化
        //    InitializeGraphics(viewParam);

        //    //背景形状を描画
        //    DrawBackGround();

        //    //各タイルを描画
        //    DrawMap(tileList, filter);

        //    //タイル枠描画
        //    DrawTileBorder(tileList);

        //    //選択座標点追加描画
        //    DrawPoint(selectedLatLon);

        //    //ルート形状描画
        //    DrawRouteGeometry();

        //    //中心十字描画

        //    //描画エリア更新
        //    UpdateImage();
        //}

        public int DrawMapObj(Graphics g, CmnObjHandle objHdl, ViewParam viewParam)
        {
            drawApi.DrawObj(g, objHdl, viewParam);

            return 0;
        }


        public void RefreshDrawArea()
        {
            viewAccess.RefreshDrawArea();
        }


        public void ShowAttribute(CmnObjHandle objHdl)
        {
            //objHdl = objHdl.obj.ToCmnObjHandle(objHdl.tile);
            viewAccess.DispListView(objHdl.GetAttributeListItem());
           // viewAccess.DispListView(objHdl.obj.GetAttributeListItem(objHdl.tile));

        }

        public void SetSelectedObjHdl(CmnObjHandle objHdl)
        {
            drawApi.selectObjHdl = objHdl;
        }

        public void SetSelectedAttr(CmnObjHandle selectAttr)
        {
            drawApi.selectAttr = selectAttr;
        }

        //public void SetBoundaryGeometry(List<LatLon[]> boundaryList)
        //{
        //    drawApi.boundaryList = boundaryList;
        //            }

        public void SetRelatedObj(List<CmnObjHdlRef> refObjList)
        {
            drawApi.refObjList = refObjList;
        }

        //public void SetRouteGeometry(LatLon[] routeGeometry)
        //{
        //    drawApi.routeGeometry = routeGeometry;
        //}


        //public void SetRouteObjList(List<CmnDirObjHandle> routeObjList)
        //{
        //    drawApi.routeObjList = routeObjList;
        //}

        /* ステータスバー用 ***************************************************************/

        public void UpdateCenterLatLon(LatLon latlon)
        {
            viewAccess.DispCenterLatLon(latlon);
        }

        public void UpdateCenterTileId(uint tileId)
        {
            viewAccess.DispCurrentTileId(tileId);
        }

        public void UpdateClickedLatLon(LatLon latlon)
        {
            viewAccess.DispClickedLatLon(latlon);
        }

        public void SetBoundaryList(List<LatLon[]> boundaryList)
        {
            this.boundaryList = boundaryList;
        }

        public void SetRouteGeometry(LatLon[] routeGeometry)
        {
            this.routeGeometry = routeGeometry;
        }

        public void SetSelectedLatLon(LatLon latlon)
        {
            this.selectedLatLon = latlon;
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

        //描画エリア
        void UpdateImage(Image newImage);
        void RefreshDrawArea();

        //属性表示
        void DispListView(List<AttrItemInfo> listItem);

        //ステータスバー
        void DispCurrentTileId(uint tileId);
        void DispCenterLatLon(LatLon latlon);
        void DispClickedLatLon(LatLon latlon);


        //ViewModel GetViewModel();

        //Image GetDrawAreaImage();

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


    /* 描画用抽象クラス ****************************************************************************************/
    public abstract class CmnDrawApi
    {
        //個別描画用
        public CmnObjHandle selectObjHdl = null;
        public CmnObjHandle selectAttr = null;
        public List<CmnObjHdlRef> refObjList = null;
        public LatLon selectPoint;
        //public List<CmnDirObjHandle> routeObjList = null;

        public InteractorSettings settings;
        //public bool isDrawOneWay = true;

        /* 描画 ==================================================================================*/

        //public LatLon[] routeGeometry = null;


        //オブジェクト描画
        public virtual void DrawObj(Graphics g, CmnObjHandle objHdl, ViewParam viewParam)
        {
            PointF[] pointF = CalcPolylineInDrawArea(objHdl.Geometry, viewParam);

            Pen pen = GetPen(objHdl);
            g.DrawLines(pen, pointF);

            return;
        }
    
        //デフォルト描画スタイル
        public virtual Pen GetPen(CmnObjHandle objHdl)
        {
            if (ReferenceEquals(selectObjHdl, objHdl))
                return new Pen(Color.Red, (float)5.0);

            //else if (routeObjList?.Count(x => x.obj.Id == obj.Id) > 0)
            //    return new Pen(Color.DarkMagenta, (float)5.0);

            else if (refObjList?.Count(x => ReferenceEquals(x.objHdl, objHdl)) > 0)
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

        public void DrawPolyline(Graphics g, LatLon[] polyline, Pen pen, ViewParam viewParam)
        {
            if (polyline == null)
                return;
            PointF[] pointF = CalcPolylineInDrawArea(polyline, viewParam);

            g.DrawLines(pen, pointF);
            return;
        }

        public void DrawPolyline(Graphics g, LatLon[] polyline, ViewParam viewParam)
        {
            if (polyline == null)
                return;
            PointF[] pointF = CalcPolylineInDrawArea(polyline, viewParam);

            Pen pen = new Pen(Color.FromArgb(96, 255,0,0), 20);
            pen.CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(2, 2);

            g.DrawLines(pen, pointF);
            return;
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



        public virtual RangeFilter<ushort> GetFilter(uint number) => null;

        public virtual CmnObjFilter SetFilter(ref CmnObjFilter filter, uint objType, RangeFilter<ushort> subFilter)
        {
            return filter.AddRule(objType, subFilter);
        }
    }




}
