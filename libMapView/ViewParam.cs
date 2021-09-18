using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Akichko.libGis;

namespace Akichko.libMapView
{
    public class ViewParam
    {

        public LatLon viewCenter;

        double zoom; // dot/m
        public int zoomLevel;

        private int width;
        private int height;

        //計算で生成
        float width_2;
        float height_2;


        public int Width {
            get { return width; }
            set {
                width = value;
                width_2 = (float)(width / 2.0);
            }
        }
        public int Height {
            get { return height; }
            set {
                height = value;
                height_2 = (float)(height / 2.0);
            }
        }
        public double Zoom
        {
            get { return zoom; }
            set
            {
                zoom = value;
                dotPerLon = mPerLon * zoom;
                dotPerLat = mPerLat * zoom;
            }
        }

        public float Width_2 => width_2;
        public float Height_2 => height_2;

        double mPerLon; // 1 => m/lon
        double mPerLat; // m/lon

        //work
        double dotPerLon;
        double dotPerLat;


        public ViewParam(double lat, double lon, double zoom)
        {
            viewCenter = new LatLon(lat, lon);
            this.zoom = zoom;

            CalcMPerLatLon();
        }

        public void SetWidthHeight(int width, int height)
        {
            this.Width = width;
            this.Height = height;
        }

        public double GetDotPerLon() => dotPerLon;
        //{
        //    return zoom * mPerLon;
        //}

        public double GetDotPerLat() => dotPerLat;
        //{
        //    return zoom * mPerLat;
        //}

        public LatLon GetLatLon(int offsetX, int offsetY)
        {
            //XYを緯度経度に変換
            LatLon tmpLatLon = new LatLon();
            tmpLatLon.lon = viewCenter.lon + offsetX / GetDotPerLon();
            tmpLatLon.lat = viewCenter.lat - offsetY / GetDotPerLat();

            return tmpLatLon;
        }

        public void SetViewCenter(LatLon latlon)
        {

            //緯度経度更新
            viewCenter.lon = latlon.lon;
            viewCenter.lat = latlon.lat;

            //その他パラメータ更新
            //tileId = MapTool.CalcTileId(viewCenter);
            CalcMPerLatLon();

        }

        public void MoveViewCenter(LatLon relLatlon)
        {

            //緯度経度更新
            viewCenter.lon += relLatlon.lon;
            viewCenter.lat += relLatlon.lat;

            //その他パラメータ更新
            //tileId = MapTool.CalcTileId(viewCenter);
            CalcMPerLatLon();

        }

        public void MoveViewCenter(int x, int y)
        {
            //XYを緯度経度に変換
            double relLon = - x / GetDotPerLon();
            double relLat = y / GetDotPerLat();


            //緯度経度更新
            viewCenter.lon += relLon;
            viewCenter.lat += relLat;

            //その他パラメータ更新
            //tileId = MapTool.CalcTileId(viewCenter);
            CalcMPerLatLon();

        }


        public Bitmap CreateBitmap()
        {
            return new Bitmap(width, height);
        }

        public Graphics CreateGraphics()
        {
            return Graphics.FromImage(new Bitmap(width, height));

        }

        //中心座標から、１度あたりのｍを更新する
        private void CalcMPerLatLon()
        {
            mPerLon = viewCenter.GetDistanceTo(new LatLon(viewCenter.lat, viewCenter.lon + 1.0));
            mPerLat = viewCenter.GetDistanceTo(new LatLon(viewCenter.lat + 1.0, viewCenter.lon));

            dotPerLon = mPerLon * zoom;
            dotPerLat = mPerLat * zoom;

        }


    }


}
