using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akichko.libGis;

namespace Akichko.libMapView
{
    public abstract class CmnMapDiSetting
    {

        public abstract CmnMapMgr MapMgrFactory();
        public abstract CmnRouteMgr RouteMgrFactory(CmnMapMgr mapMgr, uint type = 0);
        public abstract CmnDrawApi DrawApiFactory();
    }



}
