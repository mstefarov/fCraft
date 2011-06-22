using System;

namespace fCraft {
    interface INotifiesOnChange {
        event EventHandler Changed;
    }
}
