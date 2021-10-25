/*============================================================================
MIT License

Copyright (c) 2021 akichko

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
============================================================================*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.ComponentModel;
using Akichko.libGis;

namespace Akichko.libMapView
{
    public class Presenter : IOutputBoundary
    {
        protected IViewApi viewAccess;
        protected CmnDrawApi drawApi;
        // protected ViewParam viewParam;

        //Bitmap drawAreaBitmap;
        //Graphics g;

        public InteractorSettings settings;
        public InteractorSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                drawApi.settings = value;
            }
        }

        //public LatLon selectedLatLon;
        public LatLon[] routeGeometry = null; //削除予定
        public List<LatLon[]> boundaryList = null; //削除予定

        //パラメータ
        public bool isDrawTileBorder = true;
        //public bool isDrawOneWay = true;


        public Presenter(IViewApi mainForm)
        {
            viewAccess = (IViewApi)mainForm;
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
            drawApi.InitializeGraphics(viewParam);
            //drawAreaBitmap = viewParam.CreateBitmap();
            //g = Graphics.FromImage(drawAreaBitmap);
            //this.viewParam = viewParam;
        }

        //タイルリスト
        public void DrawTiles(List<CmnTile> tileList, CmnObjFilter filter, ViewParam viewParam, long timeStamp)
        {
            //各タイルを描画
            foreach (CmnTile drawTile in tileList)
            {
                drawTile.GetObjGroupList(filter)
                    .Where(x => x.isDrawable)
                    .Select(x => x.GetIEnumerableDrawObjs(filter?.GetSubFilter(x.Type)))
                    .Where(x => x != null)
                    .SelectMany(x => x)
                    .Where(x => x.CheckTimeStamp(timeStamp))
                    ?.ForEach(x => DrawMapObj(x.ToCmnObjHandle(drawTile), viewParam));
            }
        }

        //背景
        public void DrawBackGround(ViewParam viewParam)
        {
            //背景形状を描画
            if (boundaryList != null && settings.isAdminBoundaryDisp)
            {
                Pen pen = new Pen(Color.Gray, 1);
                boundaryList.ForEach(x =>
                {
                    drawApi.DrawPolyline(x, pen, viewParam);
                });
            }
        }

        public void DrawTileBorder(List<CmnTile> tileList, ViewParam viewParam)
        {
            if (settings.isTileBorderDisp)
            {
                tileList.ForEach(x => drawApi.DrawObj(x.ToCmnObjHandle(x), viewParam));
            }
        }

        //座標点追加描画
        public void DrawPoint(LatLon latlon, ViewParam viewParam, PointType type = PointType.None)
        {
            switch (type)
            {
                case PointType.Clicked:
                    drawApi.DrawPoint(latlon, new PointStyle(Color.DodgerBlue, 6, Color.Yellow, 3), viewParam);
                    break;

                case PointType.Selected:
                    drawApi.DrawPoint(latlon, new PointStyle(Color.Black, 6, Color.Green, 4), viewParam);
                    break;

                case PointType.Nearest:
                    drawApi.DrawPoint(latlon, new PointStyle(Color.Black, 6, Color.Red, 4), viewParam);
                    break;

                case PointType.None:
                    drawApi.DrawPoint(latlon, new PointStyle(Color.DodgerBlue, 6, Color.White, 3), viewParam);
                    break;

                default:
                    drawApi.DrawPoint(latlon, new PointStyle(Color.DodgerBlue, 6, Color.White, 3), viewParam);
                    break;
            };
        }

        //経路計算結果描画
        //public void DrawRouteGeometry(ViewParam viewParam)
        //{
        //    //ルート形状描画
        //    Pen pen = new Pen(Color.FromArgb(96, 255, 0, 0), 20);
        //    pen.CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(2, 2);
        //    drawApi.DrawPolyline(g, routeGeometry, pen, viewParam);
        //}

        //ルート形状描画
        public void DrawRouteGeometry(LatLon[] polyline, ViewParam viewParam)
        {
            drawApi.DrawPolyline(polyline, new LineStyle(Color.FromArgb(96, 255, 0, 0), 20, true), viewParam);
        }

        //描画エリア中心＋描画
        public void DrawCenterMark(ViewParam viewParam)
        {
            float size = 5;
            if (settings.isCenterMarkDisp)
            {
                Pen pen = new Pen(Color.Red, 2);
                PointF[] points = {
                    new PointF(viewParam.Width_2 - size, viewParam.Height_2),
                    new PointF(viewParam.Width_2 + size, viewParam.Height_2) };

                drawApi.DrawLines(points, pen);

                points = new PointF[]{
                    new PointF(viewParam.Width_2, viewParam.Height_2 - size),
                    new PointF(viewParam.Width_2, viewParam.Height_2 + size) };

                drawApi.DrawLines(points, pen);

            }
        }

        //描画結果反映
        public void UpdateImage()
        {
            viewAccess.UpdateImage(drawApi.GetDrawAreaBitMap());
        }


        public int DrawMapObj(CmnObjHandle objHdl, ViewParam viewParam)
        {
            drawApi.DrawObj(objHdl, viewParam);

            return 0;
        }


        public void RefreshDrawArea()
        {
            viewAccess.RefreshDrawArea();
        }


        public void ShowAttribute(CmnObjHandle objHdl)
        {
            viewAccess.DispListView(objHdl.GetAttributeListItem());
        }

        public void SetSelectedObjHdl(CmnObjHandle objHdl, PolyLinePos nearestPos = null)
        {
            viewAccess.DispSelectedObjHdl(objHdl, nearestPos);
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

        public void SetNearestLatLon(LatLon latlon)
        {
            //viewAccess.DispClickedLatLon(latlon);
        }

        public void SetBoundaryList(List<LatLon[]> boundaryList)
        {
            this.boundaryList = boundaryList;
        }

        //public void SetRouteGeometry(LatLon[] routeGeometry)
        //{
        //    this.routeGeometry = routeGeometry;
        //}

        //public void SetSelectedLatLon(LatLon latlon)
        //{
        //    this.selectedLatLon = latlon;
        //}

        public void PrintLog(int logType, string logStr)
        {
            viewAccess.PrintLog(logType, logStr);
        }

        public void OutputRoute(IEnumerable<CmnObjHandle> route)
        {
            viewAccess.DispRoute(route.ToList());
        }



        //public void DispDest(CmnObjHandle linkHdl)
        //{
        //    viewAccess.DispDest($"{linkHdl.tile.tileId}-{linkHdl.linkIndex}");
        //}
    }



    public interface IViewApi
    {
        //描画エリア
        void UpdateImage(Image newImage);
        void RefreshDrawArea();

        //選択オブジェクト属性
        void DispListView(List<AttrItemInfo> listItem);
        void DispSelectedObjHdl(CmnObjHandle objHdl, PolyLinePos nearestPos);

        //パラメータ表示
        void DispCurrentTileId(uint tileId);
        void DispCenterLatLon(LatLon latlon);
        void DispClickedLatLon(LatLon latlon);

        void DispRoute(List<CmnObjHandle> route);

        //ログ出力
        void PrintLog(int logType, string logStr);
    }

    /* 描画用抽象クラス ****************************************************************************************/
    public abstract class CmnDrawApi
    {
        protected Bitmap drawAreaBitmap;
        protected Graphics g;

        //個別描画用
        public CmnObjHandle selectObjHdl = null;
        public CmnObjHandle selectAttr = null;
        public List<CmnObjHdlRef> refObjList = null;

        public InteractorSettings settings;

        /* 描画 ==================================================================================*/

        //初期化
        public void InitializeGraphics(ViewParam viewParam)
        {
            drawAreaBitmap = viewParam.CreateBitmap();
            g = Graphics.FromImage(drawAreaBitmap);
            //this.viewParam = viewParam;
        }

        public Image GetDrawAreaBitMap() => drawAreaBitmap;

        //オブジェクト描画
        public virtual void DrawObj(CmnObjHandle objHdl, ViewParam viewParam)
        {
            PointF[] pointF = CalcPolylineInDrawArea(objHdl.Geometry, viewParam);

            Pen pen = GetPen(objHdl);
            g.DrawLines(pen, pointF);
            pen.Dispose();
            return;
        }

        //デフォルト描画スタイル
        public virtual Pen GetPen(CmnObjHandle objHdl)
        {
            //選択中オブジェクト
            if (ReferenceEquals(selectObjHdl, objHdl))
                return new Pen(Color.Red, (float)5.0);

            //else if (routeObjList?.Count(x => x.obj.Id == obj.Id) > 0)
            //    return new Pen(Color.DarkMagenta, (float)5.0);


            //属性リスト選択中オブジェクト
            else if (refObjList?.Count(x => ReferenceEquals(x.objHdl, objHdl)) > 0)
                return new Pen(Color.DarkGreen, (float)4.0);

            return new Pen(Color.Black, 1);
        }


        //ライン描画
        public void DrawPolyline(LatLon[] polyline, Pen pen, ViewParam viewParam)
        {
            if (polyline == null)
                return;
            PointF[] pointF = CalcPolylineInDrawArea(polyline, viewParam);

            if (polyline.Length == 1)
                g.DrawLines(pen, pointF);
            else
                g.DrawLines(pen, pointF);

            return;
        }

        public void DrawPolyline(LatLon[] polyline, LineStyle style, ViewParam viewParam)
        {
            Pen pen = new Pen(style.color, style.width);
            if (style.isArrowEndCap)
                pen.CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(2, 2);

            DrawPolyline(polyline, pen, viewParam);
        }

        public void DrawLines(PointF[] pointF, Pen pen)
        {
            g.DrawLines(pen, pointF);
        }


        //ポイント描画
        public virtual void DrawPoint(LatLon latlon, ViewParam viewParam)
        {
            if (latlon == null)
                return;
            PointF pointF = CalcPointInDrawArea(latlon, viewParam);

            //色は暫定
            DrawFillCircle(pointF, Color.DodgerBlue, 6);
            DrawFillCircle(pointF, Color.Yellow, 3);
        }

        public virtual void DrawPoint(LatLon latlon, PointStyle style, ViewParam viewParam)
        {
            if (latlon == null)
                return;
            PointF pointF = CalcPointInDrawArea(latlon, viewParam);

            //色は暫定
            DrawFillCircle(pointF, style.outerColor, style.outerRadius);
            DrawFillCircle(pointF, style.innerColor, style.innerRadius);
        }

        protected virtual void DrawFillCircle(PointF pointF, Color color, float radius)
            => g.FillEllipse(new SolidBrush(color), pointF.X - radius, pointF.Y - radius, radius * 2, radius * 2);


        //座標変換
        protected PointF CalcPointInDrawArea(LatLon latlon, ViewParam viewParam)
        {
            //相対緯度経度算出
            double relLat = latlon.lat - viewParam.viewCenter.lat;
            double relLon = latlon.lon - viewParam.viewCenter.lon;

            return new PointF(
                (float)(viewParam.Width_2 + relLon * viewParam.GetDotPerLon()),
                (float)(viewParam.Height_2 - relLat * viewParam.GetDotPerLat()));

        }

        protected PointF[] CalcPolylineInDrawArea(LatLon[] geometry, ViewParam viewParam)
        {
            return geometry.Select(x => CalcPointInDrawArea(x, viewParam)).ToArray();
        }



        //public virtual RangeFilter<ushort> GetFilter(uint number) => null;

        //public virtual CmnObjFilter SetFilter(ref CmnObjFilter filter, uint objType, RangeFilter<ushort> subFilter)
        //{
        //    return filter.AddRule(objType, subFilter);
        //}
    }

    public class PointStyle
    {
        public Color outerColor;
        public int outerRadius;
        public Color innerColor;
        public int innerRadius;

        public PointStyle(Color outerColor, int outerRadius, Color innerColor, int innerRadius)
        {
            this.outerColor = outerColor;
            this.outerRadius = outerRadius;
            this.innerColor = innerColor;
            this.innerRadius = innerRadius;
        }
    }

    public class LineStyle
    {
        public Color color;
        public int width;
        public bool isArrowEndCap;

        public LineStyle(Color color, int width, bool isArrowEndCap = false)
        {
            this.color = color;
            this.width = width;
            this.isArrowEndCap = isArrowEndCap;
        }
    }

    public class LogArray
    {
        string[] logStrArray;

        public LogArray(int logNum)
        {
            logStrArray = new string[logNum];
        }

        public void UpdateLogStr(int logType, string logStr)
        {
            logStrArray[logType] = logStr;
        }

        public override string ToString()
        {
            string retStr = "";
            for (int i = 0; i < logStrArray.Length; i++)
            {
                retStr += $"[{i}]{logStrArray[i]} ";
            }

            return retStr;
        }
    }


    /* 不要？ ************************************************************/

    public class ViewModel
    {
        //PictureBox
        Image pbDrawAreaImage;

        //StatusBar
        int centerTileId;
        LatLon clickedLatLon;

        private LatLon _centerLatLon;


        public LatLon centerLatLon
        {
            get { return _centerLatLon; }

            set
            {
                _centerLatLon = value;

            }
        }

    }

    public class ViewModel2 : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        //PictureBox
        Image pbDrawAreaImage;

        //StatusBar
        int centerTileId;
        LatLon clickedLatLon;

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

    }


}
