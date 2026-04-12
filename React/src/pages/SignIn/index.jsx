import { App, Card, Button, Form, Tabs, Modal, Input, Spin, Table, Typography, Space } from 'antd';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '@hooks';
import { TextInput, PasswordInput } from '@components/inputs';
import { useState, useEffect } from 'react';
import Api, { useExceptionNotification } from '@services/api';

const SignIn = () => {
  const navigate = useNavigate();
  const { message } = App.useApp();
  const { signIn, register } = useAuth();
  const [loading, setLoading] = useState(false);
  const [loginForm] = Form.useForm();
  const [regForm] = Form.useForm();
  const { showError } = useExceptionNotification();

  const [allUsersOpen, setAllUsersOpen] = useState(false);
  const [allUsersLoading, setAllUsersLoading] = useState(false);
  const [allUsers, setAllUsers] = useState([]);

  const [lookupOpen, setLookupOpen] = useState(false);
  const [lookupLogin, setLookupLogin] = useState('');
  const [lookupLoading, setLookupLoading] = useState(false);
  const [lookupUser, setLookupUser] = useState(null);

  useEffect(() => {
    if (!allUsersOpen) return;
    let cancelled = false;
    (async () => {
      setAllUsersLoading(true);
      try {
        const { data } = await Api.get('/api/temp/users');
        if (cancelled) return;
        if (data?.success) setAllUsers(Array.isArray(data.data) ? data.data : []);
        else showError({ response: { status: 400, data: { message: data?.message, errors: data?.errors } } });
      } catch (e) {
        if (!cancelled) showError(e);
      } finally {
        if (!cancelled) setAllUsersLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
    // showError vem do hook de notificação; não incluir para evitar refetch em loop
    // eslint-disable-next-line react-hooks/exhaustive-deps -- só recarregar ao abrir o modal
  }, [allUsersOpen]);

  const handleLookup = async () => {
    const q = lookupLogin.trim();
    if (!q) {
      message.warning('Informe usuário ou e-mail');
      return;
    }
    setLookupLoading(true);
    setLookupUser(null);
    try {
      const { data } = await Api.get('/api/temp/users/lookup', {
        params: { login: q },
      });
      if (data?.success) {
        setLookupUser(data.data);
        message.success('Utilizador encontrado');
      } else {
        showError({ response: { status: 400, data: { message: data?.message, errors: data?.errors } } });
      }
    } catch (e) {
      showError(e);
    } finally {
      setLookupLoading(false);
    }
  };

  const handleLogin = async (values) => {
    setLoading(true);
    try {
      const result = await signIn(values);
      if (result.success) navigate('/');
      else showError(result.error);
    } catch (error) {
      showError(error);
    } finally {
      setLoading(false);
    }
  };

  const handleRegister = async (values) => {
    setLoading(true);
    try {
      const result = await register(values);
      if (result.success) navigate('/');
      else showError(result.error);
    } catch (error) {
      showError(error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div
      style={{
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        minHeight: '100vh',
        background: 'linear-gradient(145deg, #0f172a 0%, #1e3a5f 50%, #0d9488 100%)',
        padding: 16,
      }}
    >
      <Card
        style={{
          width: 'min(420px, 100%)',
          boxShadow: '0 12px 40px rgba(0,0,0,0.25)',
          borderRadius: 12,
        }}
      >
        <Tabs
          items={[
            {
              key: 'login',
              label: 'Entrar',
              children: (
                <Form form={loginForm} layout="vertical" onFinish={handleLogin}>
                  <TextInput
                    name="username"
                    label="Usuário"
                    placeholder="Seu login"
                    rules={[{ required: true, message: 'Obrigatório' }]}
                  />
                  <PasswordInput
                    name="password"
                    label="Senha"
                    placeholder="Sua senha"
                    rules={[{ required: true, message: 'Obrigatório' }]}
                  />
                  <Button type="primary" block htmlType="submit" loading={loading} style={{ marginTop: 8 }}>
                    Entrar
                  </Button>
                  <Space direction="vertical" size="small" style={{ width: '100%', marginTop: 16 }}>
                    <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                      Ferramentas temporárias (remover em produção)
                    </Typography.Text>
                    <Space wrap>
                      <Button size="small" onClick={() => setAllUsersOpen(true)}>
                        Listar utilizadores
                      </Button>
                      <Button
                        size="small"
                        onClick={() => {
                          setLookupOpen(true);
                          setLookupUser(null);
                          setLookupLogin('');
                        }}
                      >
                        Procurar utilizador
                      </Button>
                    </Space>
                  </Space>
                </Form>
              ),
            },
            {
              key: 'reg',
              label: 'Criar conta',
              children: (
                <Form form={regForm} layout="vertical" onFinish={handleRegister}>
                  <TextInput
                    name="userName"
                    label="Usuário"
                    placeholder="Escolha um login"
                    rules={[{ required: true, message: 'Obrigatório' }]}
                  />
                  <TextInput
                    name="email"
                    label="E-mail"
                    placeholder="voce@email.com"
                    rules={[
                      { required: true, message: 'Obrigatório' },
                      { type: 'email', message: 'E-mail inválido' },
                    ]}
                  />
                  <PasswordInput
                    name="password"
                    label="Senha"
                    placeholder="Mín. 8 caracteres, maiúscula, minúscula, número e símbolo"
                    rules={[{ required: true, message: 'Obrigatório' }]}
                  />
                  <Button type="primary" block htmlType="submit" loading={loading} style={{ marginTop: 8 }}>
                    Registrar e entrar
                  </Button>
                </Form>
              ),
            },
          ]}
        />
      </Card>

      <Modal
        title="Utilizadores (temporário)"
        open={allUsersOpen}
        onCancel={() => setAllUsersOpen(false)}
        footer={
          <Button type="primary" onClick={() => setAllUsersOpen(false)}>
            Fechar
          </Button>
        }
        width={720}
        destroyOnClose
      >
        {allUsersLoading ? (
          <div style={{ textAlign: 'center', padding: 24 }}>
            <Spin />
          </div>
        ) : (
          <Table
            size="small"
            rowKey="id"
            pagination={{ pageSize: 8 }}
            dataSource={allUsers}
            columns={[
              { title: 'ID', dataIndex: 'id', ellipsis: true },
              { title: 'Usuário', dataIndex: 'userName' },
              { title: 'E-mail', dataIndex: 'email', ellipsis: true },
              {
                title: 'Ativo',
                dataIndex: 'active',
                width: 80,
                render: (v) => (v ? 'Sim' : 'Não'),
              },
            ]}
          />
        )}
      </Modal>

      <Modal
        title="Procurar utilizador (temporário)"
        open={lookupOpen}
        onCancel={() => setLookupOpen(false)}
        footer={[
          <Button key="close" onClick={() => setLookupOpen(false)}>
            Fechar
          </Button>,
          <Button key="search" type="primary" loading={lookupLoading} onClick={handleLookup}>
            Buscar
          </Button>,
        ]}
        destroyOnClose
        width={480}
      >
        <Space direction="vertical" style={{ width: '100%' }} size="middle">
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            Nome de usuário ou e-mail (mesmo critério do login).
          </Typography.Text>
          <Input
            placeholder="usuário ou email@dominio.com"
            value={lookupLogin}
            onChange={(e) => setLookupLogin(e.target.value)}
            onPressEnter={handleLookup}
            allowClear
          />
          {lookupUser && (
            <pre
              style={{
                margin: 0,
                padding: 12,
                borderRadius: 8,
                background: 'var(--ant-color-fill-quaternary, rgba(0,0,0,0.04))',
                fontSize: 12,
                overflow: 'auto',
                maxHeight: 220,
              }}
            >
              {JSON.stringify(lookupUser, null, 2)}
            </pre>
          )}
        </Space>
      </Modal>
    </div>
  );
};

export default SignIn;
