using UnityEngine;

namespace CombatSystem {
    public class Hurtbox : MonoBehaviour, IHurtbox {
        [SerializeField] bool _active = true;
        private IHurtResponder _hurtResponder;

        public bool Active => _active;

        public IHurtResponder HurtResponder { 
            get => _hurtResponder;
            set => _hurtResponder = value;
        }

        public virtual bool ValidateHit(Hit hit) {
            return _hurtResponder != null;
        }
    }
}
