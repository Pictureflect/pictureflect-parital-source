using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PictureflectPartialSource.SynchronizationHelpers {

    /** A thread-safe boolean wrapper supporting CompareExchange. Remember to test for equality using Value, not the instance reference. */
    public class InterlockedBoolean {

        int internalValue; //Should be accessed only via interlocked. 0 means false and 1 true.

        public InterlockedBoolean(bool initialValue = false) {
            internalValue = initialValue ? 1 : 0;
        }

        public bool Value {
            get {
                return Interlocked.CompareExchange(ref internalValue, 0, 0) == 1;
            }
            set {
                Interlocked.Exchange(ref internalValue, value ? 1 : 0);
            }
        }

        public bool CompareExchange(bool value, bool comparand) {
            return Interlocked.CompareExchange(ref internalValue, value ? 1 : 0, comparand ? 1 : 0) == 1;
        }

    }

}
