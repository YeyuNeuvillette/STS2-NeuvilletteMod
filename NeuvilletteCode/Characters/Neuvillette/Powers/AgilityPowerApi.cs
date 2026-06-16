using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Neuvillette.Characters.Neuvillette.Powers;

public static class AgilityPowerApi
{
    public static bool IsReady => true;

    public static PowerModel GetModel()
    {
        return ModelDb.Power<AgilityPower>();
    }
}