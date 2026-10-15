from pathlib import Path
import json,sys,subprocess,wave,hashlib
import numpy as np
ROOT=Path(__file__).resolve().parent;PROJECT=ROOT.parents[3]
sys.path.insert(0,str(PROJECT/'output/prologue-film/tools'))
import imageio_ffmpeg
FF=imageio_ffmpeg.get_ffmpeg_exe()
def run(args):return subprocess.run([FF,'-hide_banner','-y']+args,check=True,capture_output=True)
def samples(p):return np.frombuffer(run(['-v','error','-i',str(p),'-ar','16000','-ac','1','-f','f32le','-']).stdout,'<f4')
def pitch(p):
 a=samples(p);frame=640;hop=160
 frames=np.lib.stride_tricks.sliding_window_view(a,frame)[::hop].copy()
 energy=np.sqrt(np.mean(frames**2,axis=1));frames-=frames.mean(axis=1)[:,None];frames*=np.hanning(frame)
 ac=np.fft.irfft(abs(np.fft.rfft(frames,n=2048,axis=1))**2,n=2048,axis=1)[:,:frame]
 ac/=np.maximum(ac[:,:1],1e-10);lo,hi=40,178
 values=ac[:,lo:hi];peaks=np.argmax(values,axis=1)+lo;confidence=ac[np.arange(len(ac)),peaks]
 voiced=(confidence>.6)&(energy>max(.003,np.percentile(energy,80)*.12))
 f=16000/peaks[voiced]
 return dict(median_f0_hz=float(np.median(f)),f0_p10=float(np.percentile(f,10)),f0_p90=float(np.percentile(f,90)),voiced_frames=int(sum(voiced)),duration=len(a)/16000)
gens=json.loads((ROOT/'raw/generations.json').read_text());reports=[];chosen=[]
for page in [2,4,5]:
 candidates=[]
 for g in [g for g in gens if g['page']==page]:
  info=pitch(ROOT/'raw'/f'{g["id"]}.mp3');candidates.append(dict(id=g['id'],**info))
 pick=next(x for x in candidates if x['id']=={2:'6sJpAeAdvO8kpRWVlne8',4:'wg37YTMaKZmlVDPXKjt4',5:'HJZS7j5ZFhsZgvL9kD5W'}[page]);chosen.append((page,pick['id']))
 old=pitch(ROOT.parent/'v2'/f'page{page:02}.wav')
 reports.append(dict(page=page,candidates=candidates,selected=pick['id'],v2_pitch=old,selection_note='优先较低的基频中位数；此粗略声学筛选不代表主观情感听审。'))
(ROOT/'candidate-analysis.json').write_text(json.dumps(reports,ensure_ascii=False,indent=2));(ROOT/'selected.json').write_text(json.dumps(chosen,indent=2))
processing=[]
for i,ident in chosen:
 stem=f'page{i:02}';src=ROOT/'raw'/f'{ident}.mp3';dst=ROOT/f'{stem}.wav';raw=ROOT/'raw'/f'{stem}-original.wav'
 run(['-i',str(src),'-ar','44100','-ac','1','-c:a','pcm_s16le',str(raw)])
 log=run(['-i',str(raw),'-af','loudnorm=I=-20:TP=-3:LRA=11:print_format=json','-f','null','-']).stderr.decode()
 m=json.loads(log[log.rfind('{'):log.rfind('}')+1])
 filt='loudnorm=I=-20:TP=-3:LRA=11:linear=true:'+':'.join(f'{k}={m[v]}' for k,v in [('measured_I','input_i'),('measured_TP','input_tp'),('measured_LRA','input_lra'),('measured_thresh','input_thresh'),('offset','target_offset')])+',adelay=450,apad=pad_dur=1.0'
 run(['-i',str(raw),'-af',filt,'-ar','44100','-ac','1','-c:a','pcm_s16le',str(dst)])
 run(['-i',str(dst),'-c:a','libmp3lame','-b:a','192k',str(ROOT/f'{stem}.mp3')])
 with wave.open(str(dst)) as f:dur=f.getnframes()/f.getframerate()
 (ROOT/f'{stem}-timings.json').write_text(json.dumps(dict(page=i,duration=dur,leading_padding=.45,trailing_padding=1.0),indent=2))
 processing.append(dict(page=i,generation_id=ident,duration=dur,normalization=m,source_sha256=hashlib.sha256(src.read_bytes()).hexdigest()))
with wave.open(str(ROOT/'chapter01-ending-full.wav'),'wb') as f:
 f.setnchannels(1);f.setsampwidth(2);f.setframerate(44100)
 for i in range(1,6):
  with wave.open(str(ROOT/f'page{i:02}.wav')) as w:f.writeframes(w.readframes(w.getnframes()))
run(['-i',str(ROOT/'chapter01-ending-full.wav'),'-c:a','libmp3lame','-b:a','192k',str(ROOT/'chapter01-ending-full.mp3')])
(ROOT/'processing-report.json').write_text(json.dumps(processing,ensure_ascii=False,indent=2))
for r in reports:
 pick=next(x for x in r['candidates'] if x['id']==r['selected']);print(r['page'],'previous F0',round(r['v2_pitch']['median_f0_hz']),'new F0',round(pick['median_f0_hz']),'duration',pick['duration'])
