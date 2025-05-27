namespace CustomUtils.Sound.NewSoundSystem {
    
    [TestRequired]
    public interface ISoundTrackEvent {

        public void Unload();
        public bool IsValid();
    }
}