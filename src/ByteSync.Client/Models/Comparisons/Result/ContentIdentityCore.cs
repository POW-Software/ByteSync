namespace ByteSync.Models.Comparisons.Result
{
    public class ContentIdentityCore
    {
        public ContentIdentityCore()
        {

        }

        public string? SignatureHash { get; set; }
        
        public long Size { get; set; }
       

        protected bool Equals(ContentIdentityCore other)
        {
            return SignatureHash == other.SignatureHash && Size == other.Size;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ContentIdentityCore) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((SignatureHash != null ? SignatureHash.GetHashCode() : 0) * 397) ^ Size.GetHashCode();
            }
        }
    }
}
