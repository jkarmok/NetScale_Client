using System.Collections;
using UnityEngine.UIElements;

namespace UI.Transitions
{
    public interface ITransition
    {
        IEnumerator AnimateIn(float duration);
        IEnumerator AnimateOut(float duration);
    }
}