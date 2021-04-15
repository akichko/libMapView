using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using libGis;

namespace libMapView
{
    public class ViewParam
    {
        public LatLon viewCenter;

        public double zoom; // dot/m
        public int zoomLevel;

        public int width;
        public int height;

        double mPerLon; // 1 => m/lon
        double mPerLat; // m/lon

        public ViewParam(double lat, double lon, double zoom)
        {
            viewCenter = new LatLon(lat, lon);
            this.zoom = zoom;

            CalcMPerLatLon();
        }

        public double GetDotPerLon()
        {
            return zoom * mPerLon;
        }

        public double GetDotPerLat()
        {
            return zoom * mPerLat;
        }

        public LatLon GetLatLon(int offsetX, int offsetY)
        {
            //XYを緯度経度に変換
            LatLon tmpLatLon = new LatLon();
            tmpLatLon.lon = viewCenter.lon + offsetX / GetDotPerLon();
            tmpLatLon.lat = viewCenter.lat - offsetY / GetDotPerLat();

            return tmpLatLon;
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

        }


    }


}
