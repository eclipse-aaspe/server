namespace IO.Swagger.Models
{
    public class PaginationParameters
    {
        const int MAX_RESULT_SIZE = 100;



        private int _cursor = 0;

        private int _limit = MAX_RESULT_SIZE;

        public PaginationParameters(string? cursor, int? limit)
        {
            if (string.IsNullOrEmpty(cursor))
            {
                Cursor = _cursor = 0;
            }
            else if (!string.IsNullOrEmpty(cursor))
            {
                int.TryParse(cursor, out _cursor);
                Cursor = _cursor;
            }

            if (limit != null)
            {
                Limit = limit.Value;
            }
        }

        /**
        * The maximum size of the result list.
        */
        public int Limit
        {
            get
            {
                return _limit;
            }
            set
            {
                //_limit = (value > MAX_RESULT_SIZE) ? MAX_RESULT_SIZE : value;
                _limit = value;
            }
        }

        /**
         * The position from which to resume a result listing.
         */
        public int Cursor
        {
            get
            {
                return _cursor;
            }
            set
            {
                // TODO (jtikekar, 2023-09-04): @Andreas about Base64Encoding. May need to decode

                _cursor = value;
            }
        }

    }
}
