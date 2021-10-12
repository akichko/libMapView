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
        protected IInputBoundary interactor;

        /* 初期設定 **********************************************************/

        public Controller(IInputBoundary interactor)
        {
            this.interactor = interactor;
            //this.interactor = new Interactor(new Presenter(outputClass));
            this.interactor.SetViewCenter(new LatLon(35.4629, 139.62657));
            //this.interactor.SetViewSettings(settings);
        }

        public void SetDrawInterface(CmnDrawApi drawApi)
        {
            interactor.SetDrawInterface(drawApi);
        }

        public void OpenFile(string fileName, CmnMapMgr mapMgr)
        {
            interactor.OpenFile(fileName, mapMgr);
        }

        public void Shutdown()
        {
            interactor.Shutdown();
        }


        public void SetViewSettings(InteractorSettings settings)
        {
            interactor.SetViewSettings(settings);
            RefreshDrawArea();
        }

        /* 描画パラメータ設定 **********************************************************/

        public void SetDrawAreaSize(int width, int height)
        {
            interactor.SetDrawAreaSize(width, height);
            interactor.RefreshDrawArea();
        }

        public void SetViewCenter(LatLon latlon)
        {
            interactor.SetViewCenter(latlon);
            RefreshDrawArea();
        }

        public void MoveViewCenter(int x, int y)
        {
            interactor.MoveViewCenter(x, y);
            RefreshDrawArea();
        }

        public void ChangeZoom(int delta, int x, int y)
        {
            if (delta > 0)
            {
                interactor.ChangeZoom(2, x, y);
            }
            else if (delta < 0)
            {
                interactor.ChangeZoom(0.5, x, y);
            }
            RefreshDrawArea();

        }

        public void ChangeSetting(ControlMenu menu)
        {

        }

        public enum ControlMenu
        {
            DispOneWayON,
            DispOneWayOFF,
            DispTileBorderON,
            DispTileBorderOFF,
            BiggerDispArea,
            SmallerDispArea
        }

        /* 描画 **************************************************************/

        public void Paint()
        {
            interactor.Paint();
        }

        public void RefreshDrawArea()
        {
            interactor.RefreshDrawArea();
        }

        public void SetSelectedLatLon(LatLon latlon)
        {
            interactor.SetSelectedLatLon(latlon);
        }

        public void SetRouteGeometry(LatLon[] routeGeometry)
        {
            interactor.SetRouteGeometry(routeGeometry);
        }

        public void SetBoundaryGeometry(List<LatLon[]> boundaryList)
        {
            interactor.SetBoundaryGeometry(boundaryList);
        }

        //public void SetRouteObjList(List<CmnDirObjHandle> routeObjList)
        //{
        //    interactor.SetRouteObjList(routeObjList);
        //}

        //選択

        public void LeftClick(int x, int y)
        {
            LatLon clickedLatLon = interactor.GetLatLon(x, y);
            CmnObjHandle nearestObj = interactor.SearchObject(clickedLatLon);

            //PolyLinePos nearestPos = LatLon.CalcNearestPoint(clickedLatLon, nearestObj?.Geometry);

            interactor.SetClickedLatLon(clickedLatLon);
            //interactor.SetNearestObj(nearestPos);
            RefreshDrawArea();
        }

        public void AttrClick()
        {

        }



        //属性表示

        public void ShowAttribute()
        { }


        //検索
        public void SearchObject(uint tileId, uint objType, UInt64 objId)
        {
            interactor.SearchObject(tileId, objType, objId);
            interactor.RefreshDrawArea();

        }

        public void SearchObject(uint tileId, uint objType, UInt16 objIndex)
        {
            CmnObjHandle searchedObjHdl = interactor.SearchObject(tileId, objType, objIndex);
            if (searchedObjHdl != null)
            {
                LatLon latlon = searchedObjHdl.GetCenterLatLon();
                if (latlon != null)
                    interactor.SetViewCenter(latlon);
            }
            interactor.RefreshDrawArea();

        }

        public void SearchObject(CmnSearchKey key)
        {
            CmnObjHandle searchedObjHdl = interactor.SearchObject(key);
            if (searchedObjHdl != null)
            {
                LatLon latlon = searchedObjHdl.GetCenterLatLon();
                if (latlon != null)
                    interactor.SetViewCenter(latlon);
            }
            interactor.RefreshDrawArea();

        }


        public void SelectAttribute(CmnSearchKey key)
        {
            CmnObjHandle attrObjHdl = interactor.SearchAttrObject(key);
            interactor.SetSelectedAttr(attrObjHdl);
        }


        public RouteResult CalcRoute(LatLon orgLatLon, LatLon dstLatLon)
        {
            return interactor.CalcRoute(orgLatLon, dstLatLon);
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
