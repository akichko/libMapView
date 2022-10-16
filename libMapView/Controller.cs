﻿/*============================================================================
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

namespace Akichko.libMapView
{
    public class Controller
    {
        protected Interactor interactor;
        protected Interactor interactorBg;
        protected Interactor interactorPtr;
        ViewParam viewParam;

        /* 初期設定 **********************************************************/

        public Controller(Interactor interactor, ViewParam viewParam = null)
        {
            this.interactor = interactor;
            interactorPtr = interactor;

            this.viewParam = viewParam ?? new ViewParam(35.4629, 139.62657, 1.0);
            interactor.SetViewParam(viewParam);
            //this.interactor.SetViewCenter(new LatLon(35.4629, 139.62657));
            //this.interactor.SetViewSettings(settings);

        }

        //public void SetDrawInterface(CmnDrawApi drawApi)
        //{
        //    interactor.SetDrawInterface(drawApi);
        //}

        public void OpenFile(string fileName, CmnMapMgr mapMgr, CmnDrawApi drawApi)
        {
            mapMgr.Connect(fileName);
            interactor.SetMapMgr(mapMgr, drawApi);
        }

        public void SetInteractorBg(Interactor interactorBg)
        {
            this.interactorBg = interactorBg;
            interactorBg.SetViewParam(viewParam);
        }

        public void OpenBgFile(string fileName, CmnMapMgr mapMgr, CmnDrawApi drawApi)
        {
            mapMgr.Connect(fileName);
            interactorBg.SetMapMgr(mapMgr, drawApi);
        }

        public void ChangeInteractor(bool front)
        {
            if (front)
                interactorPtr = interactor;
            else //bg
                interactorPtr = interactorBg;
        }

        public void Shutdown()
        {
            interactor.Shutdown();
            interactorBg?.Shutdown();
        }

        public void SetViewSettings(InteractorSettings settings)
        {
            interactorPtr.SetSettings(settings);
            RefreshDrawArea();
        }


        /* 描画パラメータ設定 **********************************************************/

        public void SetDrawAreaSize(int width, int height)
        {
            viewParam.Width = width;
            viewParam.Height = height;
            interactorPtr.SetViewParam(viewParam);
            RefreshDrawArea();
        }

        public void SetViewCenter(LatLon latlon)
        {
            interactor.SetViewCenter(latlon);
            interactorBg?.SetViewCenter(latlon);
            RefreshDrawArea();
        }

        public void MoveViewCenter(int x, int y)
        {
            viewParam.MoveViewCenter(x, y);
            interactor.SetViewParam(viewParam);
            interactorBg?.SetViewParam(viewParam);
            RefreshDrawArea();
        }

        public void MoveViewCenter(LatLon relLatLon)
        {
            viewParam.MoveViewCenter(relLatLon);
            interactor.SetViewParam(viewParam);
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

            interactorPtr.SetViewParam(viewParam);
            MoveViewCenter(relLatLon);


            RefreshDrawArea();

        }


        public void SetDrawPoint(PointType pointType, LatLon latlon)
        {
            interactorPtr.SetDrawPoint(pointType, latlon);
        }


        /* 描画 **************************************************************/

        public void Paint()
        {

            if (interactorBg == null)
            {
                interactor.Paint();
            }
            else // bg有
            {
                if (!interactor.Status.isPaintNeeded && !interactorBg.Status.isPaintNeeded)
                    return;

                interactor.Status.isPaintNeeded = true;
                interactorBg.Status.isPaintNeeded = true;

                interactorBg.MakeImage();

                if (interactor.status.drawEnable)
                {
                    interactor.MakeImage(interactorBg);
                    interactor.UpdateImage();
                }
                else
                {
                    interactorBg.UpdateImage();
                }
            }

        }

        public void RefreshDrawArea()
        {
            interactor.RefreshDrawArea();
            interactorBg?.RefreshDrawArea();
        }


        public void SetRouteGeometry(LatLon[] routeGeometry)
        {
            interactorPtr.SetRouteGeometry(routeGeometry);
        }

        public void SetBoundaryGeometry(List<LatLon[]> boundaryList)
        {
            interactorPtr.SetBoundaryGeometry(boundaryList);
        }

        public void ClearParam()
        {
            interactor.ClearStatus();                
        }

        //選択

        public void LeftClick(int x, int y)
        {
            LatLon clickedLatLon = viewParam.GetLatLon(x, y);
            CmnObjHandle nearestObj = interactorPtr.SearchObject(clickedLatLon);

            //PolyLinePos nearestPos = LatLon.CalcNearestPoint(clickedLatLon, nearestObj?.Geometry);

            interactorPtr.SetDrawPoint(PointType.Clicked, clickedLatLon);
            interactorPtr.SetDrawPoint(PointType.AttrSelected, null);
            RefreshDrawArea();
        }


        //属性表示

        public void ShowAttribute()
        { }


        /* 検索 **************************************************************/

        public void SearchObject(uint tileId, uint objType, UInt64 objId, bool jump = true)
        {
            CmnObjHandle searchedObjHdl = interactorPtr.SearchObject(tileId, objType, objId);
            if (searchedObjHdl != null && jump)
            {
                LatLon latlon = searchedObjHdl.GetCenterLatLon();
                if (latlon != null)
                    interactorPtr.SetViewCenter(latlon);
            }
            interactorPtr.RefreshDrawArea();

        }

        public void SearchObject(uint tileId, uint objType, UInt16 objIndex, bool jump = true)
        {
            CmnObjHandle searchedObjHdl = interactor.SearchObject(tileId, objType, objIndex);
            if (searchedObjHdl != null && jump)
            {
                LatLon latlon = searchedObjHdl.GetCenterLatLon();
                if (latlon != null)
                    interactorPtr.SetViewCenter(latlon);
            }
            interactorPtr.RefreshDrawArea();

        }

        public void SearchObject(CmnSearchKey key)
        {
            CmnObjHandle searchedObjHdl = interactor.SearchObject(key);
            if (searchedObjHdl != null)
            {
                LatLon latlon = searchedObjHdl.GetCenterLatLon();
                if (latlon != null)
                    interactorPtr.SetViewCenter(latlon);
            }
            interactorPtr.RefreshDrawArea();

        }

        public void SearchRandomObject(uint objType)
        {
            CmnObjHandle obj = interactor.SearchRandomObject(objType);

            LatLon latlon = obj?.GetCenterLatLon();

            if(latlon != null)
                interactor.SetViewCenter(latlon);
        }

        public void SearchTile(uint tileId)
        {
            interactor.SetViewCenter(tileId);
        }

        public void SelectAttribute(CmnSearchKey key)
        {
            CmnObjHandle attrObjHdl = interactor.SearchAttrObject(key);
            interactorPtr.SetAttrSelectedObj(attrObjHdl);
        }


        /* その他 **************************************************************/

        public void SetRouteMgr(CmnRouteMgr routeMgr)
        {
            interactorPtr.SetRouteMgr(routeMgr);
        }

        public RouteResult CalcRoute(LatLon orgLatLon, LatLon dstLatLon)
        {
            return interactorPtr.CalcRoute(orgLatLon, dstLatLon);
        }

        public RouteResult CalcRoute(LatLon orgLatLon)
        {
            return interactorPtr.CalcRoute(orgLatLon);
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
