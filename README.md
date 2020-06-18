# FileHandlerService
The service monitors the appearance of files in the folder with source documents and should use several workflows for processing documents. The essence of processing is to count the number of letters in a document.

The main goal of the project is to try the System.Threading.Tasks.Dataflow library.

# Conditions
The following parameters are passed to the program:
1. The path to the folder with the source documents in TXT format
2. The path to the folder with the results

The program monitors the appearance of files in the folder with source documents. The program should use 4 workflows for processing documents. The essence of processing is to count the number of letters in a document. For each file from the folder with source documents, the program must create a text file of the same name with the extension txt in the folder with the results, in which the calculated number should be written.

The source documents are copied to the folder for processing by external means during the program.

The program should end only at the request of the user. Moreover, if at the time of receipt of the completion event in the workflows the documents are being processed, it is necessary to wait until the processing of these documents is completed and complete the work. Do not start processing the remaining documents.

# ToDo
1. Implement Idisposable interface for DocumentsPipeline class
2. Сhoose another api in order to monitor the creation of files
