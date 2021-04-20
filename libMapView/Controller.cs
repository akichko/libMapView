﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using libGis;

namespace libMapView
{
    public class Controller
    {
        private Interactor interactor;

        //初期設定

        public Controller(IViewApi outputClass)
        {
            interactor = new Interactor(outputClass);
            interactor.SetViewCenter(new LatLon(35.4629, 139.62657));
            RefreshDrawArea();
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

        public void SetViewerMode()
        {

        }

        public void SetInteractorSettings(InteractorSettings settings)
        {
            interactor.settings = settings;
        }

        //描画パラメータ設定

        public void SetDrawAreaSize(int width, int height)
        {
            interactor.SetDrawAreaSize(width, height);
            //interactor.RefreshDrawArea();
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



        //描画

        public void Paint(Graphics g)
        {
            interactor.Paint(g);
        }

        public void RefreshDrawArea()
        {
            interactor.RefreshDrawArea();
        }

        public void SetSelectedLatLon(LatLon latlon)
        {
            interactor.SetSelectedLatLon(latlon);
        }

        //選択

        public void LeftClick(int x, int y)
        {
            LatLon clickedLatLon = interactor.GetLatLon(x, y);
            SearchObject(clickedLatLon);
        }

        public void AttrClick()
        {

        }

        private void SearchObject(LatLon clickedLatLon)
        {
            interactor.SearchObject(clickedLatLon);
            RefreshDrawArea();
        }


        //属性表示

        public void ShowAttribute()
        { }


    }
}
