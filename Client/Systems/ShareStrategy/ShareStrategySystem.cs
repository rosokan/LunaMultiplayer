﻿using LunaClient.Events;
using LunaClient.Systems.SettingsSys;
using LunaClient.Systems.ShareProgress;
using LunaCommon.Enums;
using Strategies;
using UnityEngine;

namespace LunaClient.Systems.ShareStrategy
{
    public class ShareStrategySystem : ShareProgressBaseSystem<ShareStrategySystem, ShareStrategyMessageSender, ShareStrategyMessageHandler>
    {
        public override string SystemName { get; } = nameof(ShareStrategySystem);

        private ShareStrategyEvents ShareStrategiesEvents { get; } = new ShareStrategyEvents();

        //BailoutGrand - Exchange funds for reputation; researchIPsellout - Exchange funds for science;
        public readonly string[] OneTimeStrategies = new string[] { "BailoutGrant", "researchIPsellout" };

        protected override bool ShareSystemReady => StrategySystem.Instance != null && StrategySystem.Instance.Strategies.Count != 0 && Funding.Instance != null && ResearchAndDevelopment.Instance != null &&
                                                    Reputation.Instance != null && Time.timeSinceLevelLoad > 1f;

        protected override void OnEnabled()
        {
            if (SettingsSystem.ServerSettings.GameMode != GameMode.Career) return;

            base.OnEnabled();
            StrategyEvent.onStrategyActivated.Add(ShareStrategiesEvents.StrategyActivated);
            StrategyEvent.onStrategyDeactivated.Add(ShareStrategiesEvents.StrategyDeactivated);
        }

        protected override void OnDisabled()
        {
            if (SettingsSystem.ServerSettings.GameMode != GameMode.Career) return;

            base.OnDisabled();
            StrategyEvent.onStrategyActivated.Remove(ShareStrategiesEvents.StrategyActivated);
            StrategyEvent.onStrategyDeactivated.Remove(ShareStrategiesEvents.StrategyDeactivated);
        }
    }
}
