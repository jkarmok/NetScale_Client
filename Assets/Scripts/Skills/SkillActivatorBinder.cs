using UnityEngine;

namespace Skills
{
    public static class SkillActivatorBinder
    {
        public static void Bind(Activator activator, ISkillExecutor executor)
        {
            Vector2 lastAimPosition = Vector2.zero;

            activator.OnInstantExecuted += () =>
                executor.Execute(Vector2.zero);

            activator.OnAimStarted += pos =>
            {
                lastAimPosition = pos;
                executor.OnAimStart(pos);
            };

            activator.OnAimUpdated += pos =>
            {
                lastAimPosition = pos;
                executor.OnAimUpdate(pos);
            };

            activator.OnAimCompleted += pos =>
            {
                lastAimPosition = pos;
                executor.OnAimComplete(pos);
                executor.Execute(pos);
            };

            activator.OnAimCancelled += () =>
                executor.OnAimCancel();
            
            activator.OnChargeStarted += charge =>
                executor.OnChargeStart(charge);

            activator.OnChargeUpdated += charge =>
                executor.OnChargeUpdate(charge);

            activator.OnChargeCompleted += charge =>
            {
                executor.OnChargeComplete(charge);
                executor.ExecuteWithCharge(charge, lastAimPosition);
            };

            activator.OnStateChanged += state =>
            {
                if (state == SkillState.Ready)
                    executor.OnChargeCancel();
            };

            activator.OnCastStarted   += () => executor.OnCastStart();
            activator.OnCastUpdated   += progress => executor.OnCastUpdate(progress);
            activator.OnCastCompleted += () => executor.OnCastComplete();
            activator.OnCastCancelled += () => executor.OnCastCancel();

            activator.OnToggleChanged += isOn =>
            {
                if (isOn) executor.OnToggleOn();
                else      executor.OnToggleOff();
            };
        }
    }
}