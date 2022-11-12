using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akichko.libGis;

namespace Akichko.libMapView
{
    public class SearchResult
    {
        public CmnObjHandle objHandle;
        public PolyLinePos nearestPos;
        public List<CmnObjHdlRef> relatedHdlList;
    }

    public class MapSearch
    {
        Interactor interactor;

        CmnMapMgr mapMgr;
        InteractorSettings settings;


        public MapSearch(Interactor interactor)
        {
            this.interactor = interactor;
            mapMgr = interactor.GetMapMgr();
            settings = interactor.GetSettings();
        }

        public SearchResult SearchObject(LatLon baseLatLon)
        {
            if (!interactor.Status.drawEnable)
                return null;

            SearchResult ret = new SearchResult();

            //最近傍オブジェクト取得
            ret.objHandle = mapMgr.SearchObj(baseLatLon, settings.ClickSearchRange, settings.drawMapObjFilter, null, settings.timeStamp);
            if (ret.objHandle == null)
                return null;

            //最近傍座標計算
            ret.nearestPos = LatLon.CalcNearestPoint(baseLatLon, ret.objHandle?.Geometry);

            //選択中オブジェクト登録
            interactor.SetSelectedObjHdl(ret.objHandle);

            //選択オブジェクト属性表示
            interactor.ShowAttribute(ret.objHandle);

            //最近傍座標点描画
            interactor.SetDrawPoint(PointType.Nearest, ret.nearestPos.latLon);

            //関連オブジェクト取得
            interactor.SearchRelatedObject(ret.objHandle);

            return ret;
        }

    }
}
