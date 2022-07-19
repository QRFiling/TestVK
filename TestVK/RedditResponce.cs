namespace TestVK
{
    //эти классы были сгенерированны встроенными методами Visual Studio на основе json ответа с реддита.
    //Они являются просто шаблоном, на который накладывается текст в json формате, чтобы с данными можно было как-то работать

    class RedditResponce
    {
        public Data data { get; set; }
    }

    public class Data
    {
        public Child[] children { get; set; }
    }

    public class Child
    {
        public Data1 data { get; set; }
    }

    public class Data1
    {
        public string title { get; set; }
        public string permalink { get; set; }
        public bool stickied { get; set; }
        public Preview preview { get; set; }
    }

    public class Preview
    {
        public Image[] images { get; set; }
        public bool enabled { get; set; }
    }

    public class Image
    {
        public Source source { get; set; }
    }

    public class Source
    {
        public string url { get; set; }
    }
}
