using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC
{
    public abstract class ASContainer
    {
        public static event EventHandler<ASContainer> NewContainer;

        public List<ASTrait> Traits { get; }

        public virtual bool IsStatic { get; }

        public abstract ASMultiname QName { get; }

        public ASContainer()
        {
            Traits = new List<ASTrait>(); IsStatic = false;

            if (NewContainer != null)
            {
                NewContainer(null, this);
            }

        }

        public CodeScope _link_codescope;

        public VTable _vtable;

        public override string ToString()
        {
            int methodCount = Traits.Count(
                    t => t.Kind == TraitKind.Method ||
                         t.Kind == TraitKind.Getter ||
                         t.Kind == TraitKind.Setter);

            int slotCount = Traits.Count(t => t.Kind == TraitKind.Slot);
            int constantCount = Traits.Count(t => t.Kind == TraitKind.Constant);

            return $"{QName}, Traits: {Traits.Count}";
        }

    }
}
