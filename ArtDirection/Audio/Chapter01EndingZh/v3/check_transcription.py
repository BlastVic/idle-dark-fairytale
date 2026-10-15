from pathlib import Path
import sys,json,subprocess
ROOT=Path(__file__).resolve().parent;PROJECT=ROOT.parents[3]
sys.path.insert(0,str(PROJECT/'output/chapter01-ending-tools'))
from faster_whisper import WhisperModel
import numpy as np
sys.path.insert(0,str(PROJECT/'output/prologue-film/tools'))
import imageio_ffmpeg
model=WhisperModel('small',device='cpu',compute_type='int8',cpu_threads=6)
for i in [2,4,5]:
 p=ROOT/f'page{i:02}.wav'
 pcm=subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-v','error','-i',str(p),'-ar','16000','-ac','1','-f','f32le','-'],capture_output=True,check=True).stdout
 segments,info=model.transcribe(np.frombuffer(pcm,dtype='<f4'),language='zh',beam_size=5,word_timestamps=True,condition_on_previous_text=False)
 rows=[dict(start=s.start,end=s.end,text=s.text,words=[dict(start=w.start,end=w.end,text=w.word,probability=w.probability) for w in s.words]) for s in segments]
 (ROOT/f'page{i:02}-independent-asr.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2))
 print(i,''.join(s['text'] for s in rows),flush=True)
