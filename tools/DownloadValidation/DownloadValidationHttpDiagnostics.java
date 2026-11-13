package com.game.sts2launcher;

import android.content.Context;
import android.os.SystemClock;
import org.json.JSONArray;
import org.json.JSONObject;
import java.io.*;
import java.net.*;
import java.util.*;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicInteger;

/** Compiled only by the explicit validation Gradle input. Never logs private URLs or headers. */
final class DownloadValidationHttpDiagnostics {
    interface ReplayCall { String run(String method, String url, String headers, String body, int timeout); }
    private static final Map<String, Request> requests = new ConcurrentHashMap<>();
    private static final ThreadLocal<Request> current = new ThreadLocal<>();
    private static final AtomicInteger active = new AtomicInteger();
    private static final AtomicInteger replayIds = new AtomicInteger();
    private static File events, trackedFiles;
    private static Context context;
    private static int lostEvents;

    private static final class Request {
        final String id, source, parent, method, url, headers, body;
        final int timeout;
        final long started = SystemClock.elapsedRealtime();
        long bytes, firstByte = -1, lastByte = -1;
        volatile long finishedAt;
        int status;
        volatile boolean exited;
        volatile String response;
        Request(String id, String source, String parent, String method, String url, String headers, String body, int timeout) {
            this.id=id; this.source=source; this.parent=parent; this.method=method; this.url=url;
            this.headers=headers; this.body=body; this.timeout=timeout;
        }
    }

    static synchronized String configure(Context app, String runId) throws Exception {
        if (!runId.matches("[a-zA-Z0-9][a-zA-Z0-9-]{0,79}")) throw new IOException("Invalid diagnostic run ID");
        if (active.get()!=0) throw new IOException("Previous diagnostic workers are still active");
        context=app; requests.clear(); lostEvents=0;
        File external = new File(app.getExternalFilesDir(null), "download-validation-evidence/runs/"+runId);
        File internal = new File(app.getFilesDir(), "download-validation/"+runId);
        if ((!external.mkdirs()&&!external.isDirectory()) || (!internal.mkdirs()&&!internal.isDirectory())) throw new IOException("Diagnostic output unavailable");
        events=new File(external,"java-http-events.jsonl");
        trackedFiles=new File(internal,"tracked-http-files.txt");
        return "enabled";
    }

    static void accept(Context app, String id, String method, String url, String headers, String body, int timeout) {
        if (events==null) return;
        Request request=new Request(id,"steamkit",null,method,url,headers,body,timeout);
        requests.put(id,request); emit(request,"accepted",null);
    }
    static void workerEnter(String id) {
        Request request=requests.get(id);
        if(request==null)return;
        current.set(request); active.incrementAndGet(); emit(request,"worker-start",null);
    }
    static void workerExit(String id) {
        Request request=requests.get(id);
        if(request==null)return;
        request.finishedAt=SystemClock.elapsedRealtime();request.exited=true; active.decrementAndGet(); emit(request,"worker-exit",null); current.remove();
    }
    static void phase(String phase) { Request request=current.get(); if(request!=null)emit(request,phase,null); }
    static void headers(int status) { Request request=current.get(); if(request!=null){request.status=status;emit(request,"headers",null);} }
    static void error(Throwable error) { Request request=current.get();if(request!=null)emit(request,"exception",error); }
    static void cancel(String id) { Request request=requests.get(id);if(request!=null)emit(request,"request-cancelled",null); }
    static void response(String id,String response) { Request request=requests.get(id);if(request!=null){request.response=response;emit(request,"response-produced",null);} }

    static InputStream track(InputStream source) {
        Request request=current.get();
        if(source==null||request==null)return source;
        emit(request,"body-stream",null);
        return new FilterInputStream(source) {
            private boolean closed, eof;
            private void count(int size) {
                if(size<0){if(!eof){eof=true;emit(request,"body-eof",null);}return;}
                if(size==0)return;
                request.bytes+=size; request.lastByte=SystemClock.elapsedRealtime()-request.started;
                if(request.firstByte<0){request.firstByte=request.lastByte;emit(request,"first-byte",null);}
            }
            @Override public int read() throws IOException {int value=in.read();count(value<0?-1:1);return value;}
            @Override public int read(byte[] buffer,int offset,int length)throws IOException{int size=in.read(buffer,offset,length);count(size);return size;}
            @Override public void close()throws IOException{
                if(!closed)emit(request,"body-close-start",null);
                try{super.close();}finally{if(!closed){closed=true;emit(request,"body-closed",null);}}
            }
        };
    }
    static File createdFile(File file) {
        if(current.get()!=null&&trackedFiles!=null) synchronized(DownloadValidationHttpDiagnostics.class){
            try(Writer out=new FileWriter(trackedFiles,true)){out.write(file.getCanonicalPath()+"\n");}
            catch(Exception error){lostEvents++;}
        }
        return file;
    }

    static String replay(String kind,ReplayCall call) throws Exception {
        if(!kind.equals("manifest")&&!kind.equals("chunk"))throw new IOException("Unsupported replay category");
        Request original=requests.values().stream().filter(r->r.source.equals("steamkit")&&category(r.url).equals(kind))
            .max(Comparator.comparingLong(r->r.started)).orElseThrow(()->new IOException("No matching original request"));
        String id="direct-"+replayIds.incrementAndGet();
        Request replay=new Request(id,"direct-java",original.id,original.method,original.url,original.headers,original.body,original.timeout);
        requests.put(id,replay);emit(replay,"accepted",null);
        Thread worker=new Thread(()->{
            workerEnter(id);
            try{response(id,call.run(replay.method,replay.url,replay.headers,replay.body,replay.timeout));}
            catch(Throwable error){error(error);response(id,"{\"error\":\"Direct Java diagnostic failed\"}");}
            finally{workerExit(id);}
        },"STS2-diagnostic-"+id);
        worker.setDaemon(true);worker.start();return id;
    }
    static String takeResponse(String id) {
        Request request=requests.get(id);
        if(request==null||!request.exited)return "";
        String response=request.response;request.response=null;return response==null?"":response;
    }
    static String state() throws Exception {
        JSONArray items=new JSONArray();
        for(Request request:requests.values()) items.put(new JSONObject().put("requestId",request.id).put("source",request.source)
            .put("category",category(request.url)).put("workerExited",request.exited).put("receivedBodyBytes",request.bytes)
            .put("status",request.status).put("elapsedMs",(request.exited?request.finishedAt:SystemClock.elapsedRealtime())-request.started)
            .put("firstByteMs",request.firstByte).put("lastByteMs",request.lastByte));
        return new JSONObject().put("activeJavaWorkers",active.get()).put("lostEvents",lostEvents).put("requests",items).toString();
    }
    static String dnsAfter(String id) throws Exception {
        Request request=requests.get(id);
        if(request==null)throw new IOException("Unknown diagnostic request");
        long start=SystemClock.elapsedRealtime();JSONObject result=new JSONObject().put("requestId",id).put("phase","separate-dns-probe-after-request");
        try{
            URI uri=new URI(request.url);result.put("host",uri.getHost());JSONArray addresses=new JSONArray();
            for(InetAddress address:InetAddress.getAllByName(uri.getHost()))addresses.put(address.getHostAddress());
            result.put("addresses",addresses);
        }catch(Throwable error){result.put("exceptionChain",exceptionChain(error));}
        return result.put("elapsedMs",SystemClock.elapsedRealtime()-start).toString();
    }
    private static String category(String url){return url.contains("/manifest/")?"manifest":url.contains("/chunk/")?"chunk":"other";}
    private static JSONArray exceptionChain(Throwable error) {
        JSONArray chain=new JSONArray();
        for(int index=0;error!=null&&index<6;index++,error=error.getCause())try{
            String message=String.valueOf(error.getMessage()).replaceAll("https?://[^\\s\\)\\]\\}\"']+","<private-url>")
                .replaceAll("[A-Za-z0-9_-]{40,}\\.[A-Za-z0-9_-]+\\.[A-Za-z0-9_-]+","<token>");
            chain.put(new JSONObject().put("type",error.getClass().getName()).put("message",message));
        }catch(Exception ignored){}
        return chain;
    }
    private static synchronized void emit(Request request,String phase,Throwable error) {
        try{
            URI uri=new URI(request.url);
            JSONObject item=new JSONObject().put("requestId",request.id).put("source",request.source).put("parentRequestId",request.parent)
                .put("processId",android.os.Process.myPid())
                .put("clock","android-elapsedRealtime").put("elapsedMs",SystemClock.elapsedRealtime()-request.started)
                .put("phase",phase).put("category",category(request.url)).put("scheme",uri.getScheme()).put("host",uri.getHost())
                .put("port",uri.getPort()>0?uri.getPort():uri.getScheme().equals("https")?443:80)
                .put("connectReadTimeoutMs",request.timeout).put("activeJavaWorkers",active.get()).put("status",request.status)
                .put("receivedBodyBytes",request.bytes).put("firstByteMs",request.firstByte).put("lastByteMs",request.lastByte);
            String[] path=uri.getPath().split("/");
            if(path.length>=5&&path[1].equals("depot"))item.put("depotId",path[2]).put("contentIdentity",path[4]);
            if(error!=null)item.put("exceptionChain",exceptionChain(error));
            try(Writer out=new FileWriter(events,true)){out.write(item.toString()+"\n");}
        }catch(Exception diagnosticFailure){lostEvents++;}
    }
}
