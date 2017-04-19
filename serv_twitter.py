import sys
import glob
sys.path.append('chatbot')
sys.path.append('chatbot/ChatBot')
sys.path.append('thrift')

##############################
# Tensorflow import
##############################
from datasets.twitter import data
from datasets.twitter.data import *

import tensorflow as tf
import numpy as np
import data_utils
import seq2seq_wrapper

import importlib
importlib.reload(seq2seq_wrapper)

##############################
# Thrift import
##############################
from chatbot.ChatBot import ChatBotSvc, ttypes
from thrift.transport import TSocket
from thrift.transport import TTransport
from thrift.protocol import TBinaryProtocol
from thrift.server import TServer


##############################
# Chatbot parameter initialization
##############################
limit = {
        'maxq' : 20,
        'minq' : 0,
        'maxa' : 20,
        'mina' : 3
        }

print( "Lodaing trained parameter...")
trained_data_path = 'datasets/twitter/'
metadata, idx_q, idx_a = data.load_data(PATH=trained_data_path)
(trainX, trainY), (testX, testY), (validX, validY) = data_utils.split_dataset(idx_q, idx_a)

xseq_len = trainX.shape[-1]
yseq_len = trainY.shape[-1]
batch_size = 16
xvocab_size = len(metadata['idx2w'])  
yvocab_size = xvocab_size
emb_dim = 1024

print( "Building seq2seq...")

model = seq2seq_wrapper.Seq2Seq(xseq_len=xseq_len,
                               yseq_len=yseq_len,
                               xvocab_size=xvocab_size,
                               yvocab_size=yvocab_size,
                               ckpt_path='ckpt/twitter/',
                               emb_dim=emb_dim,
                               num_layers=3
                               )

print( "Restoring session...")							   
sess = model.restore_last_session()

class ChatBotSvcHandler:
    def __init__(self):
        self.log = {}

    def send(self, chat_message):
        message = chat_message.message
        print('client : ', message)        
        
        data_len = limit['maxq']
        idx_q = np.zeros([data_len, limit['maxq']], dtype=np.int32)
        message_tokenized = message.split(' ')
        indices = pad_seq(message_tokenized, metadata['w2idx'], limit['maxq'])        
        idx_q = idx_q + np.array(indices).reshape(limit['maxq'], 1)
        
        output = model.predict(sess, idx_q)
        output_decoded = data_utils.decode(sequence=output[0], lookup=metadata['idx2w'], separator=' ').split(' ')
        reply = ' '.join(output_decoded).replace('unk', '')
        print(reply)
        return reply;

if __name__ == '__main__':
    handler = ChatBotSvcHandler()
    processor = ChatBotSvc.Processor(handler)
    transport = TSocket.TServerSocket(host="127.0.0.1", port=5425)
    tfactory = TTransport.TBufferedTransportFactory()
    pfactory = TBinaryProtocol.TBinaryProtocolFactory()

    server = TServer.TSimpleServer(processor, transport, tfactory, pfactory)
    
    print('Starting server...')
    server.serve()
    print('done.')
