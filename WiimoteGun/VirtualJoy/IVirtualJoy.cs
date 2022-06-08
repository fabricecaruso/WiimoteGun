namespace WiimoteGun
{
    interface IVirtualJoy
    {
        bool IsEnabled { get; }

        void SetAxis(bool AxisX, int value);
        void SetButton(uint nButton, bool value);
        void CommitChanges();
    }
}