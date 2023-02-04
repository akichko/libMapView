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
using Akichko.libGis;
using System.IO;
using System.Threading;

namespace Akichko.libMapView
{
    public class Controller
    {
        protected Interactor interactorFg;
        protected Interactor interactorBg;
        protected Interactor interactorPtr;
        ViewParam viewParam;

        public MapSearch searchApi;

        /* 初期設定 **********************************************************/

        public Controller(ViewParam viewParam = null)
        {
            interactorPtr = interactorFg;

            this.viewParam = viewParam ?? new ViewParam(35.4629, 139.62657, 1.0);
        }

        public Controller(Interactor interactor, ViewParam viewParam = null)
        {
            this.interactorFg = interactor;
            interactorPtr = interactorFg;

            this.viewParam = viewParam ?? new ViewParam(35.4629, 139.62657, 1.0);
            interactorFg.SetViewParam(viewParam);
        }

        public int OpenMap(string fileName, CmnMapMgr mapMgr, CmnDrawApi drawApi, bool isForeground = true)
        {
            Interactor interactor = isForeground ? interactorFg : interactorBg;
            int ret = mapMgr.Connect(fileName);
            if (ret < 0)
                return ret;
            interactor.SetMapMgr(mapMgr, drawApi);
            return ret;
        }
        public int CloseMap(bool isForeground = true)
        {
            Interactor interactor = isForeground ? interactorFg : interactorBg;

            int ret = interactor?.Disconnect() ?? -1;
            return ret;
        }

        public void SetInteractor(Interactor interactor, bool isForeground = true)
        {
            if (isForeground)
            {
                interactorFg = interactor;
                interactorPtr = interactorFg;
            }
            else 
                interactorBg = interactor;

            interactor.SetViewParam(viewParam);
        }

        public void ChangeInteractor(bool front)
        {
            if (front)
                interactorPtr = interactorFg;
            else //bg
                interactorPtr = interactorBg;
        }

        public void Shutdown()
        {
            interactorFg?.Disconnect();
            interactorBg?.Disconnect();
        }

        public InteractorSettings GetViewSettings() => interactorPtr?.GetSettings();

        public void SetViewSettings(InteractorSettings settings)
        {
            interactorPtr?.SetSettings(settings);
            RefreshDrawArea();
        }


        /* 描画パラメータ設定 **********************************************************/

        public void SetDrawAreaSize(int width, int height)
        {
            viewParam.Width = width;
            viewParam.Height = height;
            //interactorPtr.SetViewParam(viewParam);
            RefreshDrawArea();
        }

        public void SetViewCenter(LatLon latlon)
        {
            interactorFg.SetViewCenter(latlon);
            interactorBg?.SetViewCenter(latlon);
            RefreshDrawArea();
        }

        public void MoveViewCenter(int x, int y)
        {
            viewParam.MoveViewCenter(x, y);
            interactorFg?.SetViewParam(viewParam);
            interactorBg?.SetViewParam(viewParam);

            RefreshDrawArea();
        }

        public void MoveViewCenter(LatLon relLatLon)
        {
            viewParam.MoveViewCenter(relLatLon);
            interactorFg?.SetViewParam(viewParam);
            interactorBg?.SetViewParam(viewParam);
            RefreshDrawArea();
        }

        public void ChangeZoom(int delta, int x, int y)
        {
            double zoom = delta > 0 ? 2 : 0.5;

            LatLon clickedLatLon = viewParam.GetLatLon(x, y);

            viewParam.Zoom *= zoom;

            LatLon afterLatLon = viewParam.GetLatLon(x, y);

            //マウス位置保持のための移動
            LatLon relLatLon = new LatLon(clickedLatLon.lat - afterLatLon.lat, clickedLatLon.lon - afterLatLon.lon);

            interactorPtr?.SetViewParam(viewParam);
            MoveViewCenter(relLatLon);

            //RefreshDrawArea();

        }


        public void SetDrawPoint(PointType pointType, LatLon latlon)
        {
            interactorPtr?.SetDrawPoint(pointType, latlon);
        }

        public void SetDrawLine(LineType lineType, LatLon[] geometry)
        {
            interactorPtr?.SetDrawLine(lineType, geometry);
        }

        /* 描画 **************************************************************/

        public void Paint()
        {
            //if (!interactorFg.status.drawEnable)
            //    return;

            Image img = null;

            //BG描画
            if (interactorBg != null)
            {
                if (interactorBg.Status.isPaintNeeded)
                {
                    img = interactorBg.MakeImage();

                    if (interactorFg == null)
                    {
                        interactorBg.UpdateImage(img);
                    }
                    else
                    {
                        interactorFg.Status.isPaintNeeded = true;
                    }
                }
                else if (interactorFg != null && interactorFg.Status.isPaintNeeded)
                {
                    img = interactorBg.MakeImage();
                }
            }

            //FG描画
            if (interactorFg != null && interactorFg.Status.isPaintNeeded)
            {
                interactorFg.MakeImage(img);
                interactorFg.UpdateImage();
            }
        }

        public void RefreshDrawArea()
        {
            interactorFg?.Repaint();
            interactorBg?.Repaint();

            interactorFg?.RefreshDrawArea();
            interactorBg?.RefreshDrawArea();
        }



        public void SetBoundaryGeometry(List<LatLon[]> boundaryList)
        {
            interactorPtr.SetBoundaryGeometry(boundaryList);
        }

        public void ClearParam()
        {
            interactorPtr?.ClearStatus();                
        }

        //選択

        public LatLon LeftClick(int x, int y)
        {
            LatLon clickedLatLon = viewParam.GetLatLon(x, y);

            if (interactorPtr == null)
                return clickedLatLon;

            MapSearch searchApi = new MapSearch(interactorPtr);

            CmnObjHandle nearestObj = searchApi.SearchObject(clickedLatLon)?.objHandle;
           
            //interactorPtr.SearchObject(clickedLatLon);

            //PolyLinePos nearestPos = LatLon.CalcNearestPoint(clickedLatLon, nearestObj?.Geometry);

            interactorPtr.SetDrawPoint(PointType.Clicked, clickedLatLon);
            interactorPtr.SetDrawPoint(PointType.AttrSelected, null);
            RefreshDrawArea();

            return clickedLatLon;
        }

        public LatLon GetLatLon(int x, int y) => viewParam.GetLatLon(x, y);

        //属性表示

        public void ShowAttribute()
        { }


        /* 検索 **************************************************************/

        public CmnObjHandle SearchObject(uint tileId, uint objType, UInt64 objId, bool jump = true)
        {
            CmnObjHandle searchedObjHdl = interactorPtr.SearchObject(tileId, objType, objId);
            if (searchedObjHdl != null && jump)
            {
                LatLon latlon = searchedObjHdl.GetCenterLatLon();
                if (latlon != null)
                    interactorPtr.SetViewCenter(latlon);
            }
            interactorPtr.Repaint();
            interactorPtr.RefreshDrawArea();

            return searchedObjHdl;
        }

        public CmnObjHandle SearchObject(CmnSearchKey key)
        {
            CmnObjHandle searchedObjHdl = interactorFg.SearchObject(key);
            if (searchedObjHdl != null)
            {
                LatLon latlon = searchedObjHdl.GetCenterLatLon();
                if (latlon != null)
                    interactorPtr.SetViewCenter(latlon);
            }
            interactorPtr.RefreshDrawArea();

            return searchedObjHdl;
        }

        public CmnObjHandle SearchRandomObject(uint objType)
        {
            CmnObjHandle searchedObjHdl = interactorFg.SearchRandomObject(objType);

            LatLon latlon = searchedObjHdl?.GetCenterLatLon();

            if(latlon != null)
                interactorFg.SetViewCenter(latlon);

            return searchedObjHdl;
        }

        public void SearchTile(uint tileId)
        {
            interactorFg.SetViewCenter(tileId);
        }

        public void SelectAttribute(CmnSearchKey key)
        {
            CmnObjHandle attrObjHdl = interactorFg.SearchAttrObject(key);
            interactorPtr.SetAttrSelectedObj(attrObjHdl);
        }


        /* その他 **************************************************************/

        public RouteResult CalcRoute(CmnRouteMgr routeMgr, LatLon orgLatLon, LatLon dstLatLon)
        {

            ////計算
            RouteResult routeCalcResult = routeMgr.CalcRoute(orgLatLon, dstLatLon);

            if (routeCalcResult.resultCode != ResultCode.Success)
            {
                SetDrawLine(LineType.RouteGeometry, null);

                return routeCalcResult;
            }

            List<CmnObjHandle> route = routeCalcResult.route.Select(x => x.DLinkHdl).ToList();
            LatLon[] routeGeometry = routeMgr.GetResult();

            interactorPtr.OutputRoute(route, routeGeometry);

            SetDrawLine(LineType.RouteGeometry, routeGeometry);

            return routeCalcResult;
            // return interactorPtr.CalcRoute(orgLatLon, dstLatLon);
        }

        public RouteResult CalcRoute(CmnRouteMgr routeMgr, LatLon orgLatLon)
        {
            //計算
            RouteResult routeCalcResult = routeMgr.CalcAutoRoute(orgLatLon);

            if (routeCalcResult.resultCode != ResultCode.Success)
            {
                SetDrawLine(LineType.RouteGeometry, null);

                return routeCalcResult;
            }

            //status.route = routeCalcResult.links.Select(x => x.DLinkHdl);

            LatLon[] routeGeometry = routeMgr.GetRouteGeometry(routeCalcResult.links);
            interactorPtr.OutputRoute(routeCalcResult.links, routeGeometry);

            SetDrawLine(LineType.RouteGeometry, routeGeometry);

            return routeCalcResult;
            
            //return interactorPtr.CalcRoute(orgLatLon);
        }

        public void ReadBackground(StreamReader sr)
        {

            List<LatLon[]> BoundaryList = new List<LatLon[]>();

            string fbuf;
            string sWayId;
            int shapeNo;
            double lon;
            double lat;

            List<LatLon> geometryList = new List<LatLon>();

            //データ数読み込み
            while ((fbuf = sr.ReadLine()) != null)
            {
                string[] csv_column = fbuf.Split('\t');

                //TSVformat
                int row = 0;
                sWayId = csv_column[row++];
                shapeNo = int.Parse(csv_column[row++]);
                lon = double.Parse(csv_column[row++]);
                lat = double.Parse(csv_column[row++]);

                LatLon tmpLatLon = new LatLon(lat, lon);
                geometryList.Add(tmpLatLon);

                if (shapeNo == 99999)
                {
                    BoundaryList.Add(geometryList.ToArray());
                    geometryList = new List<LatLon>();
                }
            }

            SetBoundaryGeometry(BoundaryList);
            RefreshDrawArea();
        }
    }

}
