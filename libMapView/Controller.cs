using System;
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



        //描画パラメータ設定

        public void SetDrawAreaSize(int width, int height)
        {
            interactor.SetDrawAreaSize(width, height);
            //interactor.RefreshDrawArea();
        }

        public void MoveViewCenter(int x, int y)
        {
            interactor.MoveViewCenter(x, y);
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
        }


        //属性表示

        public void ShowAttribute()
        { }


    }
}
