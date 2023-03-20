namespace IO.Swagger.Lib.V3.Models
{
    public class PaginationParameters
    {
        const int MAX_RESULT_SIZE = 100;

        /**
         * The result set should start at this value.
         */
        public int From { get; set; } = 0;

        private int _size = MAX_RESULT_SIZE;

        public PaginationParameters(int? from, int? size)
        {
            if(from != null)
            {
                From = from.Value;
            }

            if(size != null)
            {
                Size = size.Value;
            }
        }

        /**
        * The maximum size of the result set.
        */
        public int Size
        {
            get
            {
                return _size;
            }
            set
            {
                _size = (value > MAX_RESULT_SIZE) ? MAX_RESULT_SIZE : value;
            }
        }

    }
}
