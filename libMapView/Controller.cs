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

namespace Akichko.libMapView
{
    public class Controller
    {
        private IInputBoundary interactor;

        /* 初期設定 **********************************************************/

        public Controller(IViewApi outputClass, InteractorSettings settings)
        {
            interactor = new Interactor(new Presenter(outputClass));
            interactor.SetViewCenter(new LatLon(35.4629, 139.62657));
            interactor.SetViewSettings(settings);
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
            //SearchObject(clickedLatLon);
            interactor.SearchObject(clickedLatLon);

            interactor.SetClickedLatLon(clickedLatLon);
            RefreshDrawArea();
        }

        public void AttrClick()
        {

        }

        //private void SearchObject(LatLon clickedLatLon)
        //{
        //    interactor.SearchObject(clickedLatLon);
        //    RefreshDrawArea();
        //}


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
            if(searchedObjHdl != null)
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
            CmnObjHandle attrObjHdl = interactor.SearchObject(key);
            interactor.SetSelectedAttr(attrObjHdl);

        }


        public void CalcRoute(LatLon orgLatLon, LatLon dstLatLon)
        {
            interactor.CalcRoute(orgLatLon, dstLatLon);
        }
    }
}
