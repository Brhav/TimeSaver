using iText.Kernel.Pdf;
using iText.Kernel.Utils;

namespace TimeSaver
{
    public static class PdfCombiner
    {
        public static byte[] CombineIntoSinglePdf(List<string> filePaths)
        {
            var memoryStream = new MemoryStream();

            var pdfWriter = new PdfWriter(memoryStream);

            var pdfDocument = new PdfDocument(pdfWriter);

            var pdfMerger = new PdfMerger(pdfDocument);

            foreach (var filePath in filePaths)
            {
                var pdfReader = new PdfReader(filePath);

                var tempPdfDocument = new PdfDocument(pdfReader);

                pdfMerger.Merge(tempPdfDocument, 1, tempPdfDocument.GetNumberOfPages());

                tempPdfDocument.Close();
            }

            pdfDocument.Close();

            return memoryStream.ToArray();
        }
    }
}
