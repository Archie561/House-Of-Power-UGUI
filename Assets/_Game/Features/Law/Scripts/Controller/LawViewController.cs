using Game.General;
using UnityEngine;

namespace Game.Features.Law
{
    public class LawViewController : MonoBehaviour
    {
        [SerializeField] private LawView _lawView;

        private void Start()
        {
            string lawText = "In order to improve public health, we propose the implementation of a universal healthcare system that ensures access to medical services for all citizens.";
            _lawView.Initialize(DocumentType.Healthcare, lawText);
        }
    }
}
