namespace ApsCalcUI
{
    class DamageTypeItem
    {
        public DamageType ID { get; }
        public string Text { get; }

        public DamageTypeItem(DamageType iD, string text)
        {
            ID = iD;
            Text = text;
        }
    }
}
