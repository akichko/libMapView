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

        public LatLon[] routeGeometry = null; //削除予定
        public List<LatLon[]> boundaryList = null; //削除予定


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
            //this.settings = settings;
            drawApi.settings = settings;
        }


        /* 描画 ******************************************************/

        //初期化
        public void InitializeGraphics(InteractorSettings settings, ViewParam viewParam, IOutputBoundary presenter = null)
        {
            //this.settings = settings;
            drawApi.settings = settings;
            drawApi.InitializeGraphics(viewParam, ((Presenter)presenter)?.drawApi);
            //drawAreaBitmap = viewParam.CreateBitmap();
            //g = Graphics.FromImage(drawAreaBitmap);
            //this.viewParam = viewParam;
        }

        //タイルリスト
        public void DrawTiles(List<CmnTile> tileList, CmnObjFilter filter, ViewParam viewParam, long timeStamp = -1)
        {
            //各タイルを描画
            foreach (CmnTile drawTile in tileList)
            {
                drawTile.GetObjGroups(filter)
                    .Where(x => x.isDrawable)
                    .Select(x => x.GetDrawObjs(filter?.GetSubFilter(x.Type)))
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
            if (boundaryList != null)
            {
                Pen pen = new Pen(Color.Gray, 1);
                boundaryList.ForEach(x =>
                {
                    drawApi.DrawPolyline(x, pen);
                });
            }
        }

        public void DrawTileBorder(List<CmnTile> tileList, ViewParam viewParam)
        {
            tileList.ForEach(x => drawApi.DrawObj2(x.ToCmnObjHandle(x)));
        }

        //座標点追加描画
        public void DrawPoint(LatLon latlon, ViewParam viewParam, PointType type = PointType.None)
        {
            switch (type)
            {
                case PointType.Clicked:
                    drawApi.DrawPoint(latlon, new PointStyle(Color.DodgerBlue, 6, Color.Yellow, 3));
                    break;

                case PointType.Selected:
                    drawApi.DrawPoint(latlon, new PointStyle(Color.Black, 6, Color.Green, 4));
                    break;

                case PointType.Nearest:
                    drawApi.DrawPoint(latlon, new PointStyle(Color.Black, 6, Color.Red, 4));
                    break;

                case PointType.Location:
                    drawApi.DrawPoint(latlon, new PointStyle(Color.DarkGreen, 9, Color.LightGreen, 5));
                    break;

                case PointType.Origin:
                    drawApi.DrawPoint(latlon, new PointStyle(Color.Blue, 7, Color.LightBlue, 4));
                    break;

                case PointType.Destination:
                    drawApi.DrawPoint(latlon, new PointStyle(Color.Red, 7, Color.Yellow, 4));
                    break;

                case PointType.None:
                    drawApi.DrawPoint(latlon, new PointStyle(Color.DodgerBlue, 6, Color.White, 3));
                    break;

                default:
                    drawApi.DrawPoint(latlon, new PointStyle(Color.DodgerBlue, 6, Color.White, 3));
                    break;
            };
        }


        //ルート形状描画
        public void DrawRouteGeometry(LatLon[] polyline, ViewParam viewParam)
        {
            if (polyline == null)
                return;
            drawApi.DrawPolyline(polyline, new LineStyle(Color.FromArgb(96, 255, 0, 0), 20, false, true));
        }

        //描画エリア中心＋描画
        public void DrawCenterMark(ViewParam viewParam)
        {
            float size = 5;
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

        //描画結果反映
        public void UpdateImage()
        {
            viewAccess?.UpdateImage(drawApi.GetDrawAreaBitMap());
        }


        public int DrawMapObj(CmnObjHandle objHdl, ViewParam viewParam)
        {
            drawApi.DrawObj3(objHdl);

            return 0;
        }


        public void RefreshDrawArea()
        {
            viewAccess?.RefreshDrawArea();
        }


        public void ShowAttribute(CmnObjHandle objHdl)
        {
            if (objHdl == null)
                return;
            viewAccess?.DispListView(objHdl?.GetAttributeListItem());
        }

        public void SetSelectedObjHdl(CmnObjHandle objHdl, PolyLinePos nearestPos = null)
        {
            viewAccess?.DispSelectedObjHdl(objHdl, nearestPos);
            drawApi.selectObjHdl = objHdl;
        }

        public void SetSelectedAttr(CmnObjHandle selectAttr)
        {
            drawApi.selectAttr = selectAttr;
        }

        public void SetRelatedObj(List<CmnObjHdlRef> refObjList)
        {
            drawApi.refObjList = refObjList;
        }

        /* ステータスバー用 ***************************************************************/

        public void UpdateCenterLatLon(LatLon latlon)
        {
            viewAccess?.DispCenterLatLon(latlon);
        }

        public void UpdateCenterTileId(uint tileId)
        {
            viewAccess?.DispCurrentTileId(tileId);
        }

        public void UpdateClickedLatLon(LatLon latlon)
        {
            viewAccess?.DispLatLon(PointType.Clicked, latlon);
        }

        public void SetBoundaryList(List<LatLon[]> boundaryList)
        {
            this.boundaryList = boundaryList;
        }

        public void PrintLog(int logType, string logStr)
        {
            viewAccess?.PrintLog(logType, logStr);
        }

        public void OutputRoute(List<CmnObjHandle> route)
        {
            viewAccess?.DispRoute(route.ToList());
        }

        public void SetTimeStampRange(TimeStampRange timeStampRange)
        {
            viewAccess?.SetTimeStampRange(timeStampRange);
        }

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
        //void DispClickedLatLon(LatLon latlon);
        
        void DispLatLon(PointType pointType, LatLon latlon);

        void DispRoute(List<CmnObjHandle> route);

        //ログ出力
        void PrintLog(int logType, string logStr);
        void SetTimeStampRange(TimeStampRange timeStampRange);
    }


    public class CmnDrawStyleMng
    {
        protected LineStyle defaultLineStyle;
        protected Pen defaultPen;
        protected PointStyle defaultPointStyle;

        public CmnDrawStyleMng()
        {
            defaultLineStyle = new LineStyle(Color.Black, (float)1.0);
            defaultPen = defaultLineStyle.CreatePen();
            defaultPointStyle = new PointStyle(Color.DodgerBlue, 6, Color.White, 3);
        }

        public virtual Pen GetPen(CmnObjHandle objHdl, ObjDrawParam drawParam)
        {
            return defaultPen;
        }

        public virtual PointStyle GetPointStyle(CmnObjHandle objHdl, ObjDrawParam drawParam)
        {
            return defaultPointStyle;
        }
    }

    /* 描画用抽象クラス ****************************************************************************************/
    public abstract class CmnDrawApi
    {
        protected Image drawAreaBitmap;
        protected Graphics g;
        protected ViewParam viewParam;
        protected CmnDrawStyleMng drawStyle;

        //個別描画用
        public CmnObjHandle selectObjHdl = null;
        public CmnObjHandle selectAttr = null;
        public List<CmnObjHdlRef> refObjList = null;

        public InteractorSettings settings;

        /* 描画 ==================================================================================*/

        //初期化
        public void InitializeGraphics(ViewParam viewParam, CmnDrawApi preDrawApi = null)
        {
            this.viewParam = viewParam;

            if (preDrawApi != null)
            {
                drawAreaBitmap = preDrawApi.drawAreaBitmap;
                g = preDrawApi.GetGraphics;
                return;
            }

            drawAreaBitmap = viewParam.CreateBitmap();
            g = Graphics.FromImage(drawAreaBitmap);
            //this.viewParam = viewParam;
        }

        public Graphics GetGraphics => g;

        public Image GetDrawAreaBitMap() => drawAreaBitmap;

        //オブジェクト描画
        public virtual void DrawObj(CmnObjHandle objHdl)
        {
            PointF[] pointF = viewParam.CalcPolylineInDrawArea(objHdl.Geometry);

            Pen pen = GetPen(objHdl);
            g.DrawLines(pen, pointF);
            pen.Dispose();
            return;
        }

        public virtual void DrawObj3(CmnObjHandle objHdl)
        {
            ObjDrawParam drawParam = GetObjDrawParam(objHdl);

            switch (objHdl.obj.GeoType)
            {
                case GeometryType.Point:
                    PointStyle pointStyle = drawStyle.GetPointStyle(objHdl, drawParam);
                    DrawPoint(objHdl.Geometry[0], pointStyle);
                    break;

                default:
                    Pen pen = drawStyle.GetPen(objHdl, drawParam);
                    DrawPolyline(objHdl.Geometry, pen);
                    break;
            }
            return;
        }


        public virtual ObjDrawParam GetObjDrawParam(CmnObjHandle objHdl)
        {
            //選択中オブジェクト
            if (selectObjHdl != null && selectObjHdl.IsEqualTo(objHdl))
                return new ObjDrawParam(ObjDrawStatus.Selected);

            //属性リスト選択中オブジェクト
            if (selectAttr != null && selectAttr.IsEqualTo(objHdl))
                return new ObjDrawParam(ObjDrawStatus.AttrSelected);

            //関連オブジェクト
            List<int> refObjTypeList = refObjList?.Where(x => x.objHdl.IsEqualTo(objHdl)).Select(x => x.objRefType).ToList();
            if (refObjTypeList != null && refObjTypeList.Count > 0)
                return new ObjDrawParam(ObjDrawStatus.Reffered, refObjTypeList);

            //その他
            return new ObjDrawParam(ObjDrawStatus.Normal);
        }


        public virtual void DrawObj2(CmnObjHandle objHdl)
        {
            if (!(objHdl.obj is IDrawStyle))
            {
                DrawObj(objHdl);
                return;
            }

            LineStyle lineStyle;
            IDrawStyle drawObj = (IDrawStyle)objHdl.obj;

            //選択中オブジェクト
            if (selectObjHdl != null && selectObjHdl.IsEqualTo(objHdl))
                //if (ReferenceEquals(selectObjHdl, objHdl))
                lineStyle = drawObj.GetLineStyleSelected();

            //属性リスト選択中オブジェクト
            else if (selectAttr != null && selectAttr.IsEqualTo(objHdl))
                lineStyle = drawObj.GetLineStyleAttrSelected();

            //関連オブジェクト
            else if (refObjList?.Count(x => x.objHdl.IsEqualTo(objHdl)) > 0)
            {
                int refObjType = refObjList.Where(x => x.objHdl.IsEqualTo(objHdl)).First().objRefType;
                lineStyle = drawObj.GetLineStyleReffered(refObjType);
            }
            else
                lineStyle = drawObj.GetLineStyle();

            //lineStyle = new LineStyle(Color.Black, 1);


            DrawPolyline(objHdl.Geometry, lineStyle);

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
        public void DrawPolyline(LatLon[] polyline, Pen pen)
        {
            if (polyline == null)
                return;
            PointF[] pointF = viewParam.CalcPolylineInDrawArea(polyline);

            if (polyline.Length == 1)
                g.DrawLines(pen, pointF);
            else
                g.DrawLines(pen, pointF);

            return;
        }

        public void DrawPolyline(LatLon[] polyline, LineStyle style)
        {
            Pen pen = new Pen(style.color, style.width);
            if (style.isArrowEndCap)
                pen.CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(2, 2);

            DrawPolyline(polyline, pen);
        }

        public void DrawLines(PointF[] pointF, Pen pen)
        {
            g.DrawLines(pen, pointF);
        }


        //ポイント描画
        public virtual void DrawPoint(LatLon latlon)
        {
            if (latlon == null)
                return;
            PointF pointF = viewParam.CalcPointInDrawArea(latlon);

            //色は暫定
            DrawFillCircle(pointF, new SolidBrush(Color.DodgerBlue), 6);
            DrawFillCircle(pointF, new SolidBrush(Color.Yellow), 3);
        }

        public virtual void DrawPoint(LatLon latlon, PointStyle style)
        {
            if (latlon == null)
                return;
            PointF pointF = viewParam.CalcPointInDrawArea(latlon);

            //色は暫定
            DrawFillCircle(pointF, new SolidBrush(style.outerColor), style.outerRadius);
            DrawFillCircle(pointF, new SolidBrush(style.innerColor), style.innerRadius);
        }

        protected virtual void DrawFillCircle(PointF pointF, Color color, float radius)
            => g.FillEllipse(new SolidBrush(color), pointF.X - radius, pointF.Y - radius, radius * 2, radius * 2);

        protected virtual void DrawFillCircle(PointF pointF, Brush brush, float radius)
            => g.FillEllipse(brush, pointF.X - radius, pointF.Y - radius, radius * 2, radius * 2);


        protected virtual void DrawCircle(LatLon latlon, LineStyle style, float radiusMeter)
        {
            if (latlon == null)
                return;
            PointF pointC = viewParam.CalcPointInDrawArea(latlon);
            PointF pointNW = viewParam.CalcPointInDrawArea(latlon.GetOffsetLatLon(-radiusMeter, radiusMeter));

            float radiusX = pointC.X - pointNW.X;
            float radiusY = pointC.Y - pointNW.Y;

            g.DrawEllipse(new Pen(style.color, style.width), pointNW.X, pointNW.Y, radiusX * 2, radiusY * 2);

        }

    }



    public enum ObjDrawStatus
    {
        Normal,
        Selected,
        AttrSelected,
        Reffered
    }

    public class ObjDrawParam
    {
        public ObjDrawStatus status;
        public List<int> objRefType;

        public ObjDrawParam(ObjDrawStatus status, List<int> objRefType = null)
        {
            this.status = status;
            this.objRefType = objRefType;
        }
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
        public float width;
        public bool isArrowStartCap;
        public bool isArrowEndCap;

        public LineStyle(Color color, float width, bool isArrowStartCap = false, bool isArrowEndCap = false)
        {
            this.color = color;
            this.width = width;
            this.isArrowStartCap = isArrowStartCap;
            this.isArrowEndCap = isArrowEndCap;
        }


        public Pen CreatePen()
        {
            Pen pen = new Pen(color, width);
            if (isArrowStartCap)
                pen.CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(2, 2);
            if (isArrowEndCap)
                pen.CustomStartCap = new System.Drawing.Drawing2D.AdjustableArrowCap(2, 2);

            return pen;
        }
    }

    /* ログ出力 ****************************************************************************************/

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


    /* 描画スタイル ****************************************************************************************/

    public class DrawStyle<T>
    {
        protected LineStyle defaultStyle;
        protected Pen defaultPen;
        protected Dictionary<T, Pen> penDic;

        public DrawStyle()
        {
            defaultStyle = new LineStyle(Color.LightGray, (float)1.0);
            defaultPen = defaultStyle.CreatePen();

            penDic = new Dictionary<T, Pen>();
        }

        public Pen GetPen(T type)
        {
            if (penDic.ContainsKey(type))
                return penDic[type];

            return defaultPen;
        }
    }

    public abstract class ObjDrawStyle : DrawStyle<ObjDrawStatus>
    {
        LineStyle selected;
        LineStyle attSetelcted;
        Pen penSelected;
        Pen penAttSelected;

        public ObjDrawStyle()
        {
            this.selected = new LineStyle(Color.Red, (float)5.0);
            this.attSetelcted = new LineStyle(Color.Orange, (float)4.0);

            penSelected = selected.CreatePen();
            penAttSelected = attSetelcted.CreatePen();

            penDic[ObjDrawStatus.Selected] = penSelected;
            penDic[ObjDrawStatus.AttrSelected] = penAttSelected;
        }

        public Pen GetPen(CmnObjHandle objHdl, ObjDrawParam drawParam)
        {
            switch (drawParam.status)
            {
                case ObjDrawStatus.Reffered:
                    return GetRefferedPen(drawParam.objRefType);

                case ObjDrawStatus.Normal:
                    return GetNormalPen(objHdl);

                default:
                    return penDic[drawParam.status];
            }

        }
        
        public abstract Pen GetRefferedPen(List<int> reftype);
        public abstract Pen GetNormalPen(CmnObjHandle objHdl);

    }


    //削除予定
    public interface IDrawStyle
    {
        void SetArrowCap(ref LineStyle lineStyle, bool isOneWayDisp);
        LineStyle GetLineStyleSelected();
        LineStyle GetLineStyleAttrSelected();
        LineStyle GetLineStyleReffered(int objRefType);
        LineStyle GetLineStyle();
    }

    public class DefaultStyle : IDrawStyle
    {
        LineStyle selected;
        LineStyle attSelected;
        LineStyle reffered;
        LineStyle normal;

        public DefaultStyle()
        {
            this.selected = new LineStyle(Color.Red, (float)5.0);
            this.attSelected = new LineStyle(Color.DarkGreen, (float)4.0);
            this.reffered = new LineStyle(Color.DarkOrange, (float)4.0);
            this.normal = new LineStyle(Color.LightGray, (float)1.0);
        }

        public virtual void SetArrowCap(ref LineStyle lineStyle, bool isOneWayDisp) { }
        public virtual LineStyle GetLineStyleSelected() => selected;
        public virtual LineStyle GetLineStyleAttrSelected() => attSelected;
        public virtual LineStyle GetLineStyleReffered(int objRefType) => reffered;
        public virtual LineStyle GetLineStyle() => normal;
    }


    /* 不要？ ************************************************************/

    //public class ViewModel
    //{
    //    //PictureBox
    //    Image pbDrawAreaImage;

    //    //StatusBar
    //    int centerTileId;
    //    LatLon clickedLatLon;

    //    private LatLon _centerLatLon;


    //    public LatLon centerLatLon
    //    {
    //        get { return _centerLatLon; }

    //        set
    //        {
    //            _centerLatLon = value;

    //        }
    //    }

    //}

    //public class ViewModel2 : INotifyPropertyChanged
    //{
    //    public event PropertyChangedEventHandler PropertyChanged;

    //    //PictureBox
    //    Image pbDrawAreaImage;

    //    //StatusBar
    //    int centerTileId;
    //    LatLon clickedLatLon;

    //    private LatLon _centerLatLon;


    //    public LatLon centerLatLon
    //    {
    //        get
    //        {
    //            return _centerLatLon;
    //        }
    //        set
    //        {
    //            _centerLatLon = value;

    //        }
    //    }

    //}

}
