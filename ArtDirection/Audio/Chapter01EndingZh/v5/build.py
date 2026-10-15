from pathlib import Path
import sys,json,wave,subprocess,shutil
import numpy as np
P=Path(__file__).resolve().parent;OLD=P.parent/'v3';sys.path.insert(0,str(P.parents[3]/'output/prologue-film/tools'))
import imageio_ffmpeg
FF=imageio_ffmpeg.get_ffmpeg_exe();SR=44100
for pattern in ['page*.wav','page*.mp3','page*.srt','page*-script.txt','page*-timings.json','page*-independent-asr.json']:
 for f in OLD.glob(pattern):shutil.copy2(f,P/f.name)
def run(args):return subprocess.run([FF,'-v','info','-y']+args,capture_output=True,check=True)
gens=json.loads((P/'raw/generations.json').read_text());chosen=min(gens,key=lambda g:g['duration']);src=P/'raw'/f'{chosen["id"]}.mp3'
log=run(['-i',str(src),'-af','loudnorm=I=-20:TP=-3:LRA=11:print_format=json','-f','null','-']).stderr.decode();m=json.loads(log[log.rfind('{'):log.rfind('}')+1])
filt='loudnorm=I=-20:TP=-3:LRA=11:linear=true:'+':'.join(f'{k}={m[v]}' for k,v in [('measured_I','input_i'),('measured_TP','input_tp'),('measured_LRA','input_lra'),('measured_thresh','input_thresh'),('offset','target_offset')])+',adelay=200,apad=pad_dur=0.3'
run(['-i',str(src),'-af',filt,'-ar',str(SR),'-ac','1','-c:a','pcm_s16le',str(P/'quote-preview.wav')])
def read(p):
 with wave.open(str(p)) as f:return np.frombuffer(f.readframes(f.getnframes()),'<i2').copy()
def write(p,a):
 with wave.open(str(p),'wb') as f:f.setnchannels(1);f.setsampwidth(2);f.setframerate(SR);f.writeframes(a.astype('<i2').tobytes())
quote=read(P/'quote-preview.wav');edit=json.loads((OLD/'quote-edit.json').read_text());duration=edit['quote_from_page05'][1]-edit['quote_from_page05'][0]
cuts={5:edit['quote_from_page05'],2:[edit['old_cut'][0],edit['old_cut'][0]+duration]}
for i,(start,end) in cuts.items():
 a=read(OLD/f'page{i:02}.wav');left=round(start*SR);right=round(end*SR)
 head=a[:left].copy();tail=a[right:].copy();n=round(.003*SR);head[-n:]=(head[-n:]*np.linspace(1,0,n)).astype('<i2');tail[:n]=(tail[:n]*np.linspace(0,1,n)).astype('<i2')
 out=np.concatenate([head,quote,tail]);write(P/f'page{i:02}.wav',out)
 run(['-i',str(P/f'page{i:02}.wav'),'-c:a','libmp3lame','-b:a','192k',str(P/f'page{i:02}.mp3')])
 meta=json.loads((P/f'page{i:02}-timings.json').read_text());meta['duration']=len(out)/SR;(P/f'page{i:02}-timings.json').write_text(json.dumps(meta,ensure_ascii=False,indent=2))
write(P/'chapter01-ending-full.wav',np.concatenate([read(P/f'page{i:02}.wav') for i in range(1,6)]))
run(['-i',str(P/'chapter01-ending-full.wav'),'-c:a','libmp3lame','-b:a','192k',str(P/'chapter01-ending-full.mp3')])
(P/'edit-record.json').write_text(json.dumps(dict(selected=chosen,source='v3',cuts=cuts,quote_duration=len(quote)/SR,normalization=m,method='新生成整句，不加单字表演标记、不剪句内停顿。两处替换同一条新音频。'),ensure_ascii=False,indent=2))
for name in ['check_transcription.py','align_subtitles.py']:shutil.copy2(OLD/name,P/name)
f=P/'check_transcription.py';f.write_text(f.read_text().replace('for i in [2,4,5]:','for i in [2,5]:'))
print('New quote:',chosen['duration'],'seconds; inserted:',len(quote)/SR)
